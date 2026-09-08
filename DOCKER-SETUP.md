# Running with Docker

Everything — six microservices, the gateway, the frontend, SQL Server, Redis and
RabbitMQ — comes up with one command. You do not need .NET, Node, or SQL Server
installed on your machine. Docker downloads and runs all of it.

---

## 1. Install Docker Desktop

Download from https://www.docker.com/products/docker-desktop and install it.

On Windows it will ask to enable **WSL 2** — say yes and restart when prompted.

Open Docker Desktop and wait until the whale icon in the system tray stops animating and
the dashboard says **Engine running**. Then check from a terminal:

```bash
docker --version
docker compose version
```

Both must print a version. If `docker` is "not recognized", Docker Desktop is not running
or not on your PATH — restart it and open a new terminal.

**Give Docker enough memory.** SQL Server alone wants about 2 GB. Docker Desktop →
Settings → Resources → set Memory to **6 GB or more** → Apply & Restart. This is the
single most common reason the stack fails to start.

---

## 2. Create your `.env`

Open a terminal in the project folder (the one containing `docker-compose.yml`):

```bash
cd HRM-System

copy .env.example .env        # Windows CMD
cp .env.example .env          # PowerShell / macOS / Linux
```

Open `.env` and change these two lines to your own values:

```
SQL_SA_PASSWORD=Your_password123
JWT_SECRET=CHANGE_ME_super_secret_key_at_least_32_chars_long_123456
```

Rules that will bite you if ignored:

- `SQL_SA_PASSWORD` needs **8+ characters with an uppercase letter, a lowercase letter
  and a digit**. SQL Server refuses to start otherwise and the container exits.
- `JWT_SECRET` needs **at least 32 characters**. Shorter and every service crashes on
  boot with a key-size error.

The defaults technically work for a first run, but change them before you show this to
anyone.

---

## 3. Start everything

```bash
docker compose up --build
```

The first run takes **10–20 minutes**: Docker downloads the .NET SDK, the ASP.NET
runtime, Node, SQL Server, Redis and RabbitMQ images, then compiles seven .NET projects
and installs the frontend packages. Later runs take under a minute.

Leave this terminal open — it streams the logs of every container. You will see, in
roughly this order:

```
hrm-sqlserver    | SQL Server is now ready for client connections
hrm-rabbitmq     | Server startup complete
hrm-identity-service      | [INF] Identity database ready
hrm-employee-service      | [INF] Employee database ready
hrm-organization-service  | [INF] Organization database ready
hrm-attendance-service    | [INF] Attendance database ready
hrm-leave-service         | [INF] Leave database ready
hrm-notification-service  | [INF] Listening on queue notification-service.events
hrm-api-gateway  | Now listening on: http://[::]:8080
hrm-frontend     | ✓ Ready in 3.2s
```

Warnings like `Database not ready (attempt 2)` during the first minute are normal — the
services retry until SQL Server finishes booting.

To run it in the background instead, use `docker compose up --build -d` and watch logs
with `docker compose logs -f`.

---

## 4. Open the app

| What | URL |
|---|---|
| **The app** | http://localhost:3000 |
| API Gateway | http://localhost:8080 |
| Identity Swagger | http://localhost:8080/identity/swagger |
| Employee Swagger | http://localhost:8080/employee/swagger |
| Organization Swagger | http://localhost:8080/organization/swagger |
| Attendance Swagger | http://localhost:8080/attendance/swagger |
| Leave Swagger | http://localhost:8080/leave/swagger |
| RabbitMQ UI | http://localhost:15672 |

Sign in with the seeded super admin:

```
admin@hrm.local
Admin@12345
```

RabbitMQ UI uses `RABBITMQ_USER` / `RABBITMQ_PASSWORD` from your `.env` (default
`hrm` / `hrm_dev_password`).

---

## 5. Prove the whole thing is wired up

1. **Dashboard** — 24 seeded employees, the headcount chart, upcoming birthdays.
2. **Employees → Add employee** — save it. You get back a code like `EMP-2026-0025`.
3. **Notifications** — within a second or two, "New employee onboarded" appears. That
   notification travelled Employee Service → RabbitMQ → Notification Service.
4. **RabbitMQ UI → Exchanges → hrm.events → Queues** — you will see
   `notification-service.events` and `leave-service.employee-events` with messages that
   have been delivered and acknowledged.
5. **Leave → Request leave** — the balance moves into "pending" immediately. Approve it
   from the same page and it becomes "used", and a second notification arrives.
6. **Attendance → Check in** — status is Present or Late depending on the time of day.

If step 3 works, your event-driven plumbing is correct end to end.

---

## 6. Everyday commands

```bash
# stop everything (databases are kept)
docker compose down

# start again, no rebuild needed
docker compose up

# rebuild after changing C# code
docker compose up --build

# rebuild just one service
docker compose build employee-service
docker compose up -d employee-service

# follow one service's logs
docker compose logs -f employee-service

# what is running right now
docker compose ps

# open a shell inside a container
docker compose exec employee-service bash

# WIPE ALL DATA and start completely fresh
docker compose down -v
docker compose up --build
```

`down -v` deletes the volumes, so all six databases and every queue disappear and get
reseeded on the next start. Use it when data gets into a strange state.

### Frontend changes need no rebuild

`docker-compose.yml` mounts `./frontend/nextjs-app` into the container, so editing a
`.tsx` file refreshes the browser immediately. You only need
`docker compose build frontend` after adding an **npm package**.

### Backend changes do need a rebuild

C# is compiled into the image, so after editing a service:

```bash
docker compose up --build -d employee-service
```

---

## 7. When something goes wrong

**`docker: command not found` / "Docker Desktop is starting"**
Docker Desktop is not fully running. Wait for "Engine running", then reopen the terminal.

**`port is already allocated`**
Something else owns 3000, 8080, 1433, 5672, 6379 or 15672. Either stop that program, or
change the port in `.env` — for example `GATEWAY_PORT=8090`. If you change
`GATEWAY_PORT`, also change `NEXT_PUBLIC_API_URL` in `.env` to match, because the browser
uses that URL.

**`hrm-sqlserver` keeps restarting**
Almost always the password. Check `SQL_SA_PASSWORD` meets the complexity rules, then:

```bash
docker compose down -v
docker compose up --build
```

Second most likely cause is memory — raise Docker Desktop's memory limit to 6 GB.

**Services log `Database not ready` forever**
Look at the SQL Server logs: `docker compose logs sqlserver`. If it never says "ready for
client connections", it is the password or memory issue above.

**The app loads but login fails with a network error**
The gateway is not up. Check `docker compose ps` — `hrm-api-gateway` must be running, not
restarting. Then open http://localhost:8080/health; it should return `Healthy`.

**Login returns 401 on every request afterwards**
`JWT_SECRET` changed between builds while old tokens are still in your browser cookies.
Sign out, or clear cookies for localhost, then sign in again.

**Frontend shows CORS errors**
`CORS_ALLOWED_ORIGINS` in `.env` must exactly match where the frontend runs, including
the port: `http://localhost:3000`.

**Everything is very slow on Windows**
Make sure the project sits on your WSL/Windows drive rather than a network share, and
that Docker Desktop is using the WSL 2 backend (Settings → General).

---

## 8. Push to GitHub

```bash
cd HRM-System

git init
git add .
git commit -m "HRM system: gateway, six microservices, Next.js frontend, Docker"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/hrm-system.git
git push -u origin main
```

Before the first push, confirm `.env` is **not** staged:

```bash
git status --short | findstr /I ".env"     # Windows
git status --short | grep -i "\.env"        # bash
```

Only `.env.example` and `.env.local.example` should ever be committed — they contain
placeholders, not real secrets. `.gitignore` already excludes `.env`, `node_modules/`,
`bin/`, `obj/` and `.next/`.

When GitHub asks for a password, paste a **personal access token** instead: GitHub →
Settings → Developer settings → Personal access tokens → Tokens (classic) → Generate new
token → tick `repo`.

Anyone who clones the repo then runs it with exactly three commands:

```bash
cp .env.example .env
docker compose up --build
# open http://localhost:3000
```
