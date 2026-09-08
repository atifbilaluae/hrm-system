# Running locally: Visual Studio + VS Code + your own SQL Server

This guide is for running **without Docker**: backend in Visual Studio, frontend in
VS Code, and the SQL Server you already have installed. For the Docker route see the
README instead — the two are independent, you do not need both.

---

## 0. What you need installed

| Tool | Why | Check it works |
|---|---|---|
| .NET 9 SDK | builds and runs the services | `dotnet --version` → 9.x |
| Visual Studio 2022 (17.12+) | backend IDE, "ASP.NET and web development" workload | — |
| Node.js 20 or newer | runs the Next.js frontend | `node -v` → v20+ |
| VS Code | frontend IDE | — |
| SQL Server (any edition) | databases | connect from SSMS |
| Git | version control | `git --version` |

Redis and RabbitMQ are **optional** for a local run. See step 6 — the app runs without
them, just with two features degraded.

---

## 1. Ports each project uses

Already configured in each project's `Properties/launchSettings.json`:

| Project | URL |
|---|---|
| API Gateway | http://localhost:5000 |
| Identity Service | http://localhost:5001 |
| Employee Service | http://localhost:5002 |
| Organization Service | http://localhost:5003 |
| Attendance Service | http://localhost:5004 |
| Leave Service | http://localhost:5005 |
| Notification Service | http://localhost:5006 |
| Frontend | http://localhost:3000 |

The frontend only ever talks to **5000**. Everything else is behind the gateway.

---

## 2. Point the services at your SQL Server

Each service has an `appsettings.Development.json` with this connection string:

```
Server=localhost;Database=HRM_IdentityDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

**Change `Server=localhost` if your instance name is different.** Common cases:

| Your installation | Use |
|---|---|
| Default instance | `Server=localhost;` |
| SQL Server Express | `Server=localhost\\SQLEXPRESS;` |
| LocalDB | `Server=(localdb)\\MSSQLLocalDB;` |
| Named instance | `Server=localhost\\YOURINSTANCE;` |

If you use SQL authentication instead of Windows authentication, replace
`Trusted_Connection=True` with `User Id=sa;Password=YourPassword;`.

Files to edit (only the `Server=` part — leave the database names alone):

```
backend/identity-service/appsettings.Development.json
backend/employee-service/appsettings.Development.json
backend/organization-service/appsettings.Development.json
backend/attendance-service/appsettings.Development.json
backend/leave-service/appsettings.Development.json
backend/notification-service/appsettings.Development.json
```

**You do not create the databases by hand.** Each service creates and seeds its own
database on first start. You will end up with six databases: `HRM_IdentityDb`,
`HRM_EmployeeDb`, `HRM_OrganizationDb`, `HRM_AttendanceDb`, `HRM_LeaveDb`,
`HRM_NotificationDb`.

---

## 3. Open the backend in Visual Studio

1. Visual Studio → **File → Open → Project/Solution** → pick `HRM.sln` in the repo root.
2. Wait for NuGet restore to finish (bottom-left status bar).
3. **Build → Build Solution** (Ctrl+Shift+B). It must say `Build succeeded`.

### Set multiple startup projects

Right-click the **solution** in Solution Explorer → **Configure Startup Projects…** →
select **Multiple startup projects** and set Action = **Start** for these seven, in
this order (order matters only for tidy logs, not correctness):

```
HRM.IdentityService
HRM.EmployeeService
HRM.OrganizationService
HRM.AttendanceService
HRM.LeaveService
HRM.NotificationService
HRM.ApiGateway
```

Leave `HRM.Shared` and all four `*.Tests` projects as **None**.

Click OK, then press **F5** (or Ctrl+F5 to run without the debugger, which is faster).

Seven console windows open. Each one prints something like:

```
[10:12:04 INF] [HRM Identity Service] Identity database ready
Now listening on: http://localhost:5001
```

Check it: open http://localhost:5001/swagger and http://localhost:5000/health.
The gateway root http://localhost:5000 lists the routes it knows about.

---

## 4. Run the frontend in VS Code

```bash
cd frontend/nextjs-app
copy .env.local.example .env.local      # Windows CMD
# cp .env.local.example .env.local      # PowerShell / bash

npm install                             # first time only, takes a few minutes
npm run dev
```

`.env.local` must contain:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

Open http://localhost:3000 and sign in:

```
admin@hrm.local
Admin@12345
```

Useful VS Code extensions (already listed in `.vscode/extensions.json` so VS Code will
offer them): ESLint, Prettier, Tailwind CSS IntelliSense.

---

## 5. Check that it actually works

1. **Dashboard** — 24 seeded employees, headcount chart, birthdays.
2. **Employees → Add employee** — creating one returns an `EMP-2026-xxxx` code.
3. **Attendance → Check in** — status shows Present or Late depending on the time.
4. **Leave → Request leave** — balance drops into "pending" immediately.
5. **Leave → Approve** (you are SuperAdmin, so you see the approver queue) — pending
   becomes used.
6. **Notifications** — only fills up if RabbitMQ is running (next step).

---

## 6. Redis and RabbitMQ (optional, but recommended)

The system is written so a missing Redis or RabbitMQ degrades one feature instead of
crashing anything:

| Missing | What breaks | What still works |
|---|---|---|
| Redis | employee detail and dashboard stats always hit SQL | everything |
| RabbitMQ | notifications never arrive; new hires get no auto leave balances | all CRUD, attendance, leave approval |

You will see warnings like `Cache read failed` or `RabbitMQ not ready (attempt 3)` in the
console. That is the designed fallback, not a crash.

**Easiest way to get both:** install Docker Desktop, then from the repo root run the
infrastructure-only compose file. It starts Redis and RabbitMQ and nothing else — your
services keep running from Visual Studio and your own SQL Server stays untouched.

```bash
docker compose -f docker-compose.infra.yml up -d
```

Check they came up:

```bash
docker compose -f docker-compose.infra.yml ps
```

Both must show `healthy`. RabbitMQ takes about 20 seconds on first start.

Now **restart the services in Visual Studio** (Ctrl+F5 again). On boot each one connects,
and the warnings disappear. You will see instead:

```
[INF] [HRM Notification Service] Listening on queue notification-service.events
[INF] [HRM Leave Service] Listening on queue leave-service.employee-events
```

RabbitMQ management UI: http://localhost:15672 (guest / guest).

**Prove it works:** add an employee, then open the Notifications page — "New employee
onboarded" appears within a second. In the RabbitMQ UI go to **Exchanges → hrm.events →
Bindings** and you will see the two queues bound to it, with the message counts ticking
up under **Queues**.

For Redis there is nothing to watch — you just notice that opening the same employee
profile twice only hits SQL once. The second read comes from the cache for ten minutes.

Useful commands:

```bash
docker compose -f docker-compose.infra.yml logs -f rabbitmq   # follow logs
docker compose -f docker-compose.infra.yml down               # stop, keep data
docker compose -f docker-compose.infra.yml down -v            # stop, wipe queues and cache
```

**Without Docker:** install Memurai (Redis for Windows) and RabbitMQ + Erlang from their
installers. Both default to the ports above, so no config change is needed.


---

## 7. Running the tests

In Visual Studio: **Test → Test Explorer → Run All Tests**. Or from a terminal in the
repo root:

```bash
dotnet test
```

Roughly 50 tests covering shift arithmetic, leave balances, approval rules, token
rotation and employee rules. They use an in-memory database, so SQL Server does not
need to be running.

---

## 8. Push to GitHub

### First time

```bash
cd HRM-System

git init
git add .
git commit -m "HRM system: gateway, six microservices, Next.js frontend"
```

Before pushing, confirm no secrets are staged:

```bash
git status --short | findstr /I ".env"     # Windows
git status --short | grep -i "\.env"        # bash
```

Nothing should appear. `.gitignore` already excludes `.env`, `.env.local`,
`node_modules/`, `bin/`, `obj/` and `.next/`. `.env.example` and
`.env.local.example` **are** committed on purpose — they hold no real values.

Now create an empty repository on GitHub (no README, no .gitignore — you already have
both), copy its URL, then:

```bash
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/hrm-system.git
git push -u origin main
```

If GitHub asks for a password, use a **personal access token**, not your account
password: GitHub → Settings → Developer settings → Personal access tokens → Tokens
(classic) → Generate new token → tick `repo`. Paste the token as the password.

### Day to day

```bash
git add .
git commit -m "Add payroll service"
git push
```

### If you accidentally commit a secret

Rotate the secret first (change the JWT secret, the SQL password), then remove the file
from tracking and commit again:

```bash
git rm --cached .env
git commit -m "Remove committed environment file"
git push
```

A pushed secret is public even after deletion, so rotating it is the part that matters.

---

## 9. Common problems

**`A network-related or instance-specific error occurred`**
Your `Server=` value is wrong. Open SSMS, look at the server name in the connect dialog,
and copy it exactly into all six `appsettings.Development.json` files.

**`Login failed for user`**
Windows authentication is being refused. Either run Visual Studio as the same user that
has SQL access, or switch that connection string to `User Id=sa;Password=…`.

**`Address already in use` on 5000–5006**
Something else owns the port. Find it with `netstat -ano | findstr :5000`, then either
stop that process or change the port in that project's `launchSettings.json` — and if you
change a service port, update `backend/api-gateway/appsettings.Development.json` to match.

**Frontend shows "Network Error" on login**
The gateway is not running, or `.env.local` points somewhere else. The gateway must be on
5000 and `NEXT_PUBLIC_API_URL` must match. After editing `.env.local`, restart
`npm run dev` — Next.js reads it only at startup.

**401 on every request right after login**
The `Jwt:Secret` differs between Identity Service and the other services. All seven
`appsettings.Development.json` files must have the identical secret string.

**Swagger works but the frontend gets a CORS error**
`Cors:AllowedOrigins` must be `http://localhost:3000` in every service. It already is,
unless the frontend is running on a different port because 3000 was taken.

**Tables are empty / you want to start fresh**
Drop the six `HRM_*` databases in SSMS and restart the services. They rebuild and reseed
on startup.
