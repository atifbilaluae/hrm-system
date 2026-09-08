# Workforce HRM — Microservices HR platform

Next.js 15 frontend, .NET 9 microservices behind a YARP gateway, SQL Server per service,
RabbitMQ for events, Redis for caching. Everything runs with one `docker compose up`.

This repo covers Phases 1 to 5 of the roadmap at the bottom: the platform skeleton plus
six working services. Later phases add payroll, recruitment and performance — each one
repeats the pattern established here.

---

## 1. What is running

| Component | Port | Purpose |
|---|---|---|
| Frontend (Next.js) | 3000 | Dashboard UI |
| API Gateway (YARP) | 8080 | Single entry point for the browser |
| Identity Service | internal 8080 | Login, JWT, refresh tokens, roles, permissions |
| Employee Service | internal 8080 | Employee records, stats, publishes events |
| Organization Service | internal 8080 | Departments, designations, branches, holidays |
| Attendance Service | internal 8080 | Check-in/out, shift rules, corrections |
| Leave Service | internal 8080 | Types, balances, requests, approval workflow |
| Notification Service | internal 8080 | Consumes events, stores in-app notifications |
| SQL Server | 1433 | One database per service |
| RabbitMQ | 5672 / 15672 | Event bus + management UI |
| Redis | 6379 | Cache for reads that repeat |

Microservices are **not** published to the host. Only the gateway and the frontend are.
That is deliberate: nothing outside the Docker network can reach a service directly.

---

## 2. Run it

Three ways to run it. **Docker** (below, and step by step in [DOCKER-SETUP.md](DOCKER-SETUP.md))
runs everything with one command. **Local** — Visual Studio for the backend, VS Code for
the frontend, your own SQL Server — is documented in [LOCAL-SETUP.md](LOCAL-SETUP.md).
A **hybrid** of the two is the most comfortable for development: services in Visual
Studio, with only Redis and RabbitMQ in containers via
`docker compose -f docker-compose.infra.yml up -d`.

```bash
cp .env.example .env          # then edit JWT_SECRET and passwords
docker compose up --build     # first build takes a few minutes
```

Then open:

- App — http://localhost:3000 (sign in with `admin@hrm.local` / `Admin@12345`)
- Gateway — http://localhost:8080
- Identity Swagger — http://localhost:8080/identity/swagger
- Employee Swagger — http://localhost:8080/employee/swagger
- Organization Swagger — http://localhost:8080/organization/swagger
- Attendance Swagger — http://localhost:8080/attendance/swagger
- Leave Swagger — http://localhost:8080/leave/swagger
- RabbitMQ UI — http://localhost:15672 (user/password from `.env`)

Useful commands:

```bash
docker compose logs -f employee-service      # follow one service
docker compose restart notification-service
docker compose down -v                       # wipe databases and start clean
dotnet test                                  # run the xUnit suites (needs .NET 9 SDK locally)
```

The first boot creates each database, seeds roles, permissions, a super admin and
24 demo employees. That seeding is controlled by `SEED_DEMO_DATA` in `.env`.

---

## 3. Architecture

```
                        ┌──────────────────────────┐
                        │   Browser (Next.js SPA)  │
                        └────────────┬─────────────┘
                                     │ HTTPS + Bearer token
                                     ▼
                        ┌──────────────────────────┐
                        │   API Gateway (YARP)     │  validates JWT, routes by path
                        └────┬───────────┬─────────┘
             /api/auth       │           │        /api/employees
             /api/users      │           │        /api/notifications
                  ┌──────────▼──┐   ┌────▼─────────┐   ┌──────────────────┐
                  │  Identity   │   │  Employee    │   │  Notification    │
                  │  Service    │   │  Service     │   │  Service         │
                  └──────┬──────┘   └───┬──────┬───┘   └────────┬─────────┘
                         │              │      │                │
                  ┌──────▼──────┐ ┌─────▼───┐ ┌▼──────┐  ┌──────▼──────┐
                  │HRM_IdentityDb│ │HRM_Emp..│ │ Redis │  │HRM_Notif..Db│
                  └─────────────┘ └─────────┘ └───────┘  └─────────────┘
                                        │                       ▲
                                        │  publish              │ consume
                                        └────► RabbitMQ  ───────┘
                                             (hrm.events topic)
```

### Why a gateway
The browser knows one hostname. Services can be split, renamed or scaled without a
frontend change. Cross-cutting concerns (CORS, token validation, correlation ids,
later: rate limiting) live in one place instead of nine.

### Why database-per-service
`HRM_IdentityDb`, `HRM_EmployeeDb` and `HRM_NotificationDb` are separate databases with
no cross-database joins and no shared tables. A service can change its schema without
coordinating a release with anyone else, and a bad query in one service cannot lock
another service's tables.

The cost is that you lose foreign keys across boundaries. Employee Service stores
`DepartmentId` as a plain `Guid` and keeps a denormalised `DepartmentName` copy. When
Organization Service renames a department it publishes an event and Employee Service
updates its copy. Data is eventually consistent, and for HR reference data measured in
seconds that is entirely acceptable.

**Two services never talk to each other's database.** They have exactly two options:

1. **Synchronous REST** through the gateway — when the caller needs an answer right now
   (e.g. Payroll asking Employee Service to confirm an employee is active).
2. **Asynchronous events** over RabbitMQ — when the caller only needs to announce that
   something happened.

Default to option 2. Every synchronous call is a runtime dependency that can fail.

---

## 4. Event flow, end to end

Adding an employee is the shortest path through the whole system:

```
POST /api/employees                     (browser → gateway → employee-service)
   │
   ├─ validate (FluentValidation)
   ├─ generate EMP-2026-0007
   ├─ INSERT into HRM_EmployeeDb
   ├─ invalidate Redis key employee:stats
   └─ publish employee.created ──────► RabbitMQ exchange "hrm.events"
                                            │  routing key = employee.created
                                            ▼
                            queue notification-service.events
                                            │
                                            ▼
                          notification-service consumer
                             ├─ INSERT into HRM_NotificationDb
                             └─ send email (simulated in dev)
```

The HTTP response returns as soon as the row is written. The notification appears a
moment later. If Notification Service is down, messages queue up in RabbitMQ and are
processed when it comes back — the employee was still created successfully.

The exchange is a **topic** exchange. Every service binds a durable queue with the
routing keys it cares about, so adding a Leave Service later means binding a new queue,
not changing the publisher.

`HRM.Shared/Contracts/IntegrationEvents.cs` is the shared contract. It holds records
only — never EF entities. Publishing an entity would leak your schema into every other
service and turn a private refactor into a breaking change.

---

## 5. Authentication and authorization

```
1. POST /api/auth/login          → Identity Service verifies the BCrypt hash
2. Response: access token (30 min JWT) + refresh token (7 days, random 64 bytes)
3. Access token claims: userId, email, employeeId, roles[], permission[] × N
4. Browser stores both in cookies; axios attaches the access token to every call
5. Gateway validates signature/issuer/audience; downstream services validate again
6. On 401, axios calls /api/auth/refresh once, rotates the pair, replays the request
```

Two things worth understanding:

**Permissions live in the token.** `employee.create`, `leave.approve` and the rest are
claims. Employee Service authorizes locally with `[HasPermission(Permissions.EmployeeCreate)]`
and never calls Identity Service on a request path. That is what makes the services
independent. The trade-off: a permission change only takes effect when the token
refreshes, up to 30 minutes later.

**Refresh tokens are hashed and rotated.** The database stores a SHA-256 hash, never the
raw token, and using a refresh token immediately revokes it. Changing a password revokes
every active session.

Role → permission mapping lives in `identity-service/Infrastructure/DbSeeder.cs`.
Seven roles ship by default, from SuperAdmin down to Employee.

The UI reads `permissions[]` from `/api/auth/me` and hides what the user cannot do
(`<PermissionGate permission="employee.create">`). That is convenience, not security —
the API rejects the call regardless of what the UI renders.

---

## 6. Where Redis actually helps

Caching everything is worse than caching nothing, because stale data is harder to debug
than a slow query. Two places earn it here:

| Cached | Key | TTL | Why |
|---|---|---|---|
| Employee detail | `employee:{id}` | 10 min | Read on every profile view, written rarely |
| Dashboard stats | `employee:stats` | 5 min | Aggregates the whole table, loads on every login |

Employee **lists** are not cached: they are filtered and paged a hundred different ways,
so the hit rate would be near zero. Both keys are invalidated explicitly on any write.
If Redis is unavailable the service logs a warning and reads from SQL — a cache outage
degrades speed, never availability.

---

## 7. Repository layout

```
HRM-System/
├── backend/
│   ├── shared/HRM.Shared/        cross-cutting kernel used by every service
│   │   ├── Auth/                 JWT settings, permission policies, ICurrentUser
│   │   ├── Contracts/            integration events (the inter-service contract)
│   │   ├── Messaging/            RabbitMQ publisher + consumer base class
│   │   ├── Middleware/           correlation id, global exception handling
│   │   └── ServiceDefaults.cs    AddHrmDefaults / AddHrmAuth / UseHrmDefaults
│   ├── api-gateway/              YARP routes and clusters
│   ├── identity-service/         Domain / Infrastructure / Application / Controllers
│   ├── employee-service/         same layering
│   ├── organization-service/     departments, designations, branches, holidays
│   ├── attendance-service/       shift rules, check-in/out, corrections
│   ├── leave-service/            balances, approval workflow, event consumer
│   └── notification-service/     same layering + background consumer
├── frontend/nextjs-app/
│   └── src/
│       ├── app/                  routes (App Router)
│       ├── components/           ui primitives, layout, feature components
│       ├── hooks/                TanStack Query hooks
│       ├── services/             API calls — the only place that knows URLs
│       ├── schemas/              Zod validation
│       ├── types/                shared TypeScript contracts
│       └── lib/                  axios client, helpers, constants
├── tests/                        xUnit suites
├── docker-compose.yml
└── .env.example
```

Each service uses **folder-level Clean Architecture**: `Domain` (entities and rules,
no dependencies), `Infrastructure` (EF Core, Redis, external systems), `Application`
(orchestration, DTOs, validators), `Controllers` (HTTP only). Dependencies point inward.
Splitting these folders into four separate `.csproj` files per service is a mechanical
change if you want the compiler to enforce the boundaries; at three services the folder
convention is easier to navigate.

### Frontend layering
Components never call `axios`. They call a hook, the hook calls a service, the service
calls the shared client. So a page has no idea which microservice answers it, and a
change to an endpoint touches one file.

---

## 8. Docker networking, briefly

All containers join the `hrm-network` bridge. Inside it, the **service name is the
hostname**: `employee-service` reaches SQL Server at `Server=sqlserver,1433` and the
gateway reaches `http://employee-service:8080`. No IP addresses anywhere.

One thing trips everyone up: `NEXT_PUBLIC_API_URL` must be `http://localhost:8080`, not
`http://api-gateway:8080`. That URL is resolved by the **browser** on your machine, which
is not on the Docker network. Server-side code inside the frontend container would use
the internal name; client components use the published port.

`depends_on: condition: service_healthy` makes services wait for SQL Server and RabbitMQ
to pass their healthchecks. Services also retry connections on boot, because "the
container started" and "the database accepts logins" are different moments.

---

## 9. API conventions

```
GET    /api/employees?search=&status=&page=1&pageSize=10   200 paged result
GET    /api/employees/{id}                                 200 | 404
POST   /api/employees                                      201 + Location | 400 | 409
PUT    /api/employees/{id}                                 200 | 400 | 404
POST   /api/employees/{id}/terminate                       204 | 422
DELETE /api/employees/{id}                                 204 | 404
GET    /health                                             every service
```

Errors always come back in the same envelope, with a correlation id you can grep for
across all service logs:

```json
{
  "title": "Validation failed",
  "status": 400,
  "correlationId": "0f9c1c8a-...",
  "errors": { "email": ["Enter a valid email address"] }
}
```

Every request gets an `X-Correlation-Id` (generated if absent), pushed into Serilog's
context and forwarded downstream, so one user action is traceable from gateway to
service to queue consumer.

---

## 10. Development notes

**Migrations.** Services currently call `EnsureCreated()` on boot, which is fine for
development but cannot evolve a schema. Before any real deployment, switch to EF Core
migrations per service:

```bash
cd backend/employee-service
dotnet ef migrations add InitialCreate
# then replace EnsureCreatedAsync() with MigrateAsync() in Program.cs
```

**Guaranteed delivery.** `RabbitMqEventPublisher` publishes after `SaveChanges()`. If the
broker is unreachable at that instant the event is lost while the database commit stands.
The fix is the transactional outbox pattern: write the event to an `OutboxMessages` table
in the same transaction and have a background worker publish and mark it sent. That is
Phase 6 work and is deliberately not hidden here.

**Frontend hot reload** works through the mounted volume in `docker-compose.yml`. Change a
file and the page refreshes. Add an npm dependency and you need `docker compose build frontend`.

---

## 11. Business rules worth knowing

These live on the entities, not in controllers, which is why they are covered by unit
tests that need no database.

### Attendance (`AttendanceRecord`)
Shift defaults are 09:00–18:00 with a 15-minute grace period, an 8-hour standard day and
a 30-minute overtime floor.

- Arriving at 09:12 is **Present**; 09:45 is **Late by 30 minutes** — only the excess past
  the grace period counts.
- Overtime under 30 minutes is discarded, so a few stray minutes never reach payroll.
- Under 4 hours worked is a **half day**, whatever time you arrived.
- Checking in twice, or out before in, is rejected by the entity itself.
- A correction wipes the day and recalculates from the corrected times, so an approved
  correction can turn a half day back into a full day with overtime.

One database index enforces one record per employee per day. That constraint is not left
to application code.

### Leave (`LeaveBalance`, `LeaveRequest`, `LeaveCalculator`)
The balance has three numbers: **entitled**, **used**, **pending**. Available is what is
left after both.

- Submitting a request **holds** the days in `PendingDays` immediately. Two overlapping
  requests therefore cannot both fit into the same balance — the second one is refused.
- Approval moves the hold into `UsedDays`; rejection releases it. The number never leaks.
- Weekends are excluded from the day count. Public holidays are excluded too when the
  caller passes them in, which is where Organization Service's `/api/holidays` fits.
- Overlapping dates with an existing pending or approved request return **409 Conflict**.
- Nobody can approve their own request, and a decided request cannot be decided again.
- Approved leave that has already started cannot be cancelled from the UI.

### Organization (`Department`)
- Codes are unique and uppercase.
- A department cannot be its own parent, and the service walks up the tree before saving
  to make sure a move would not create a loop.
- A department with live sub-departments cannot be deleted.
- Renaming publishes `department.updated`, which is how Employee Service's denormalised
  `DepartmentName` copy stays correct.

---

## 12. The two event flows now running

**New hire → leave balances (no synchronous call anywhere)**

```
POST /api/employees
   └─ employee.created ──► RabbitMQ
                             ├─► queue leave-service.employee-events
                             │      └─ creates a balance row per active leave type
                             └─► queue notification-service.events
                                    └─ notifies the manager
```

If Leave Service is down when someone is hired, the message waits in its queue. Onboarding
does not fail, and balances appear when the service returns.

**Leave decision → the employee finds out**

```
POST /api/leave/requests/{id}/decide
   ├─ balance: pending → used (approve) or released (reject)
   └─ leave.approved / leave.rejected ──► notification-service
                                             └─ "Your leave request has been approved."
```

Both queues bind to the same `hrm.events` topic exchange. Adding Payroll later means
binding a new queue to `attendance.recorded` and `leave.approved` — no publisher changes.

---

## 13. The three run modes, and the one setting that changes

`NEXT_PUBLIC_API_URL` is inlined into the browser bundle at build time. It is resolved by
the **browser on your machine**, never by Docker's internal DNS, so it is always
`localhost` plus whichever port the gateway is published on.

| Mode | Backend | Frontend | `NEXT_PUBLIC_API_URL` |
|---|---|---|---|
| A — full local | Visual Studio (gateway on 5000) | `npm run dev` | `http://localhost:5000` |
| B — hybrid | Docker (`docker compose up`) | `npm run dev` | `http://localhost:8080` |
| C — full Docker | Docker | Docker | `http://localhost:8080` (set in `.env`) |

Mode A and B are set in `frontend/nextjs-app/.env.local`; mode C reads `.env` at the repo
root. `.env.local.example` ships both lines with one commented out.

**Mode B is the most comfortable for frontend work:** the whole backend runs in
containers and you never wait for a .NET build, while the frontend stays native and fast.
Start the backend without the frontend container:

```bash
docker compose up -d --scale frontend=0
```

**Mode A with only the infrastructure in Docker** is the most comfortable for backend
work — breakpoints in Visual Studio, but real Redis and RabbitMQ:

```bash
docker compose -f docker-compose.infra.yml up -d
```

After changing `.env.local`, restart `npm run dev`. Next.js reads it only at startup.

---

## 14. Database and migrations

Each service owns its own database and creates it on first start with
`EnsureCreatedAsync()` plus a seeder. That is deliberate for development: clone, run, get
a working system with data. It has one hard limit — **it cannot evolve a schema**. Change
an entity and the existing tables stay as they were.

Before any deployment, or as soon as two people share a database, switch to EF Core
migrations. Per service:

```bash
dotnet tool install --global dotnet-ef          # once per machine

cd backend/employee-service
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Then in that service's `Program.cs` replace:

```csharp
await db.Database.EnsureCreatedAsync();
```

with:

```csharp
await db.Database.MigrateAsync();
```

`MigrateAsync` applies any pending migrations on startup, so the Docker containers stay
self-updating. Repeat for each of the six services — they are independent, so you can
migrate one without touching the others. That independence is the whole point of
database-per-service.

Reset everything and reseed:

```bash
docker compose down -v && docker compose up --build   # Docker
# or drop the six HRM_* databases in SSMS and restart the services
```

---

## 15. Container practices used here

| Practice | Where |
|---|---|
| Multi-stage builds | every Dockerfile: restore → publish → runtime |
| Cached dependency layer | `.csproj` / `package.json` copied before source |
| Small runtime images | `aspnet:9.0` not `sdk:9.0`; `node:20-alpine` |
| Non-root containers | `USER app` (.NET), `USER nextjs` (frontend prod) |
| `.dockerignore` | `backend/`, `frontend/nextjs-app/` |
| Health checks | all 11 services, `/health` or an HTTP probe |
| Dependency ordering | `depends_on: condition: service_healthy` |
| Restart policies | `unless-stopped` on every service |
| Named volumes | SQL Server, Redis, RabbitMQ data survive `down` |
| Internal-only services | only gateway (8080) and frontend (3000) are published |
| No secrets in images | `.dockerignore` excludes `appsettings.Development.json`; everything comes from `.env` |

`backend/.dockerignore` deliberately excludes `appsettings.Development.json`. Those files
hold `localhost` connection strings for Visual Studio; inside a container they would be
wrong, and shipping developer config in an image is bad practice regardless.

---

## 16. Security notes

**Secrets.** Nothing real is committed. The values in `appsettings.json` and
`appsettings.Development.json` are development placeholders (`CHANGE_ME_...`,
`Your_password123`) that exist so a fresh clone runs. Real values live in `.env`, which
`.gitignore` excludes. In production, supply `Jwt__Secret`, `ConnectionStrings__Default`
and the RabbitMQ credentials as environment variables or from a secret manager — the
configuration system already reads them, no code change needed.

**Before deploying anywhere real:**

- Generate a fresh `JWT_SECRET` (`openssl rand -base64 48`) and a strong SQL password.
- Replace the placeholders in every `appsettings.json`, or set the env vars and leave the
  files as they are — env vars win over both files.
- Change the RabbitMQ user from the default.
- Put TLS in front of the gateway and set `CORS_ALLOWED_ORIGINS` to the real origin.
- Switch `EnsureCreated` to migrations (section 14).

**If a secret ever reaches a commit:** rotate it first — a pushed secret stays in the
history and is public even after the file is deleted. Only then remove the file with
`git rm --cached` and commit.

**Attack surface.** Only two ports are published: the gateway and the frontend. All six
microservices, SQL Server, Redis and RabbitMQ are reachable only from inside the
`hrm-network` bridge. RabbitMQ's management UI is published for convenience in
development — remove that port mapping before any shared environment.

---

## 17. Command reference

**Frontend in VS Code**
```bash
cd frontend/nextjs-app
cp .env.local.example .env.local
npm install
npm run dev                       # http://localhost:3000
npm run typecheck
npm run build
```

**Backend in Visual Studio**
```
Open HRM.sln → Ctrl+Shift+B to build
Right-click solution → Configure Startup Projects → Multiple startup projects
  → Start the six services + HRM.ApiGateway, leave HRM.Shared and *.Tests as None
Ctrl+F5 to run, F5 to debug with breakpoints
Gateway: http://localhost:5000   Swagger: http://localhost:5001/swagger
```

**Backend from the CLI**
```bash
dotnet build HRM.sln
dotnet test HRM.sln
dotnet run --project backend/identity-service
```

**Docker**
```bash
docker compose build              # build images only
docker compose up --build         # build and start everything
docker compose up -d              # start in the background
docker compose ps                 # status and health
docker compose logs -f            # all logs
docker compose logs -f employee-service
docker compose restart leave-service
docker compose down               # stop, keep data
docker compose down -v            # stop, wipe databases and queues
docker compose -f docker-compose.infra.yml up -d    # only Redis + RabbitMQ
```

**Git**
```bash
git init
git add .
git commit -m "Initial project setup"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/YOUR-REPO.git
git push -u origin main

git status --short | grep -i "^A.*\.env$"    # must print nothing before pushing
```

---

## 18. Roadmap

| Phase | Scope | Status |
|---|---|---|
| 1 | Docker infrastructure, shared kernel, gateway, Identity Service | done |
| 2 | Employee Service, Notification Service, dashboard, employee CRUD | done |
| 3 | Organization Service — departments, designations, branches, reporting lines | done |
| 4 | Attendance Service — check-in/out, working hours, corrections, reports | done |
| 5 | Leave Service — types, policies, balances, approval workflow, calendar | done |
| 6 | Payroll Service — salary structure, allowances, deductions, payslip PDFs | next |
| 7 | Recruitment Service — vacancies, candidates, Kanban pipeline, interviews | planned |
| 8 | Performance Service — goals, KPIs, reviews, self and manager evaluations | planned |
| 9 | Hardening — outbox pattern, migrations, SignalR, rate limiting, CI/CD | planned |

Phases 6 to 8 all follow the template that Employee Service establishes: entity with
behaviour, DbContext with its own database, application service, thin controller,
permission attributes, events published on state change, a Dockerfile, a gateway route,
and a matching frontend service + hooks + page.
