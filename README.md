# UIUC Research Portal & Domain-Specific LLM Assistant

Single-page institutional research portal for the University of Illinois
Urbana-Champaign (UIUC), paired with a domain-specific LLM assistant that
answers questions about recent UIUC research, publications, and faculty
expertise.

Built for the "Institutional Research Portal and Domain-Specific LLM
Assistant" technical assessment.

## Status

✅ Functional end-to-end: ingestion pipeline, API, RAG chat, and frontend have all
been run and tested against real data (not just built). See `docs/decisions.md`
for the live log of what was verified and when.

## Project Structure

```
uiuc-research-portal/
├── frontend/           Angular 17 app (portal UI + chat widget)
├── backend/            .NET 8 solution
│   ├── src/
│   │   ├── Api/              ASP.NET Core Web API (controllers)
│   │   ├── Application/      Services: OpenAlex client, RAG, LLM client
│   │   ├── Domain/           Entities
│   │   ├── Infrastructure/   EF Core DbContext, migrations, repositories
│   │   └── Ingestion/        Console app: seeds DB from OpenAlex + CSV
│   └── tests/
├── data/                Provided dataset (illinois-researchers.csv)
└── docs/
    └── decisions.md     Design decisions + problem-solving log (kept live)
```

## Tech Stack

- **Frontend**: Angular 17, Angular Material
- **Backend**: .NET 8, ASP.NET Core Web API, EF Core
- **Database**: PostgreSQL (free tier — Supabase/Neon) with `pgvector`
- **LLM**: Groq API (free tier, Llama 3.1/3.3) for answer generation
- **Embeddings**: Google Gemini `gemini-embedding-001`, 768 dimensions (free tier)
- **Data source**: [OpenAlex API](https://help.openalex.org/api/) (free, no key required to start)

All components are free-tier / free-of-cost by design (see `docs/decisions.md`).

## Data Sources

1. **Scholarly publications** — fetched live from OpenAlex, filtered to UIUC
   (`institutions.id:I157725225`), last 2 years, ~10,000–20,000 works.
2. **Researchers** — provided dataset `data/illinois-researchers.csv`
   (1,000 UIUC faculty: email, name, photo_url, position, keywords).
3. **Institutional metadata** — supplementary UIUC info gathered manually.

## How to Run (local only — no hosting account needed)

Everything in this guide runs on your own machine. No step costs money: every
service used (Postgres via Docker, Groq, Google Gemini, OpenAlex) has a free tier
that's enough for this project's scale (~1,000 researchers, ~10k–20k papers).

Commands are given for **Ubuntu/Linux** and **Windows** side by side wherever they
differ. On Windows, run the Windows commands in **PowerShell** (not Command Prompt —
some commands below use PowerShell-only syntax).

### 0. Install prerequisites

You need four things. If you already have them, skip to step 1 — check versions with
`dotnet --version` (want `8.x`), `node --version` (want `20.x` or newer), `docker --version`.

<details>
<summary><b>Ubuntu / Linux</b></summary>

```bash
# .NET 8 SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
# If apt's dotnet-sdk-8.0 package isn't available on your Ubuntu version,
# follow Microsoft's official install script instead:
# https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu

# Node.js 20+ (via nvm, so you don't need sudo for npm global installs later)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash
# restart your terminal, or: source ~/.bashrc
nvm install 20
nvm use 20

# Docker Engine — follow the official steps for your distro:
# https://docs.docker.com/engine/install/ubuntu/
# After installing, add your user to the docker group so you don't need sudo
# for every docker command (log out/in or reboot afterwards):
sudo usermod -aG docker $USER
```

</details>

<details>
<summary><b>Windows</b></summary>

```powershell
# .NET 8 SDK — download and run the installer:
# https://dotnet.microsoft.com/download/dotnet/8.0
# (Get the "SDK", not just the "Runtime".)

# Node.js 20+ — download and run the LTS installer:
# https://nodejs.org (pick the "20.x LTS" version)

# Docker Desktop for Windows (includes Docker Engine + the `docker` CLI):
# https://www.docker.com/products/docker-desktop
# During install, keep the default "Use WSL 2 instead of Hyper-V" option
# checked — it's required and it's what Docker Desktop will set up for you.
# After installing, start Docker Desktop at least once and wait for its
# whale icon in the system tray to show "Docker Desktop is running".
```

Verify each tool in a **new** PowerShell window (so updated PATH entries are picked
up): `dotnet --version`, `node --version`, `docker --version`.

</details>

### 1. Start Postgres with pgvector (Docker, free, local)

Same command on both OSes — Docker Desktop on Windows exposes the same CLI as native
Docker on Linux.

```bash
docker run -d --name uiuc-pg \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=uiuc_research_portal \
  -p 5432:5432 \
  pgvector/pgvector:pg16
```

This pulls the official `pgvector/pgvector` image (Postgres 16 with the `vector`
extension preinstalled) and starts it on `localhost:5432`. No account or sign-up
needed. First run downloads the image (~200MB), so it can take a minute depending on
your connection; later runs start instantly.

Check it's actually running before moving on:

```bash
docker ps --filter name=uiuc-pg
# should list one container, STATUS "Up ..."
```

To stop/remove it later: `docker rm -f uiuc-pg` (this also deletes its data — fine for
a local dev throwaway instance; re-run step 5 to reseed after recreating it).

_(Alternative: a hosted free-tier Postgres such as [Supabase](https://supabase.com) or
[Neon](https://neon.tech) also works — both support `pgvector`. Just use that
connection string instead in step 3 below.)_

### 2. Getting free API keys

Two keys, both free, no credit card required.

**Groq (LLM — generates the chat answers):**
1. Go to https://console.groq.com and sign in (Google/GitHub login works).
2. Open **API Keys** in the left sidebar → **Create API Key**.
3. Give it any name, copy the key (starts with `gsk_...`). You won't be able to see it
   again after closing the dialog — if you lose it, just create a new one.
4. Free tier gives a generous daily request quota — enough for this project's
   testing/demo use.

**Google Gemini (embeddings — powers semantic search over papers):**
1. Go to https://aistudio.google.com/apikey and sign in with a Google account.
2. Click **Create API key** → choose or create a Google Cloud project when prompted
   (no billing required for the free tier).
3. Copy the key (starts with `AIza...`).

Keep both keys private. **Never commit them to the repo, or paste them into
`appsettings.json`.** Step 3 below stores them with `dotnet user-secrets`, which keeps
them out of source control entirely (they're written to a JSON file in your OS user
profile, outside this repo) — so anyone who clones this repo uses their *own* free key
instead of a shared one that could get rate-limited or revoked.

### 3. Configure the backend

Run these from the repo root. `dotnet user-secrets` works identically on both OSes —
it just stores the file in a different OS-specific location under the hood.

```bash
cd backend
dotnet user-secrets init --project src/UiucResearchPortal.Api
dotnet user-secrets set "Groq:ApiKey" "<your-groq-key>" --project src/UiucResearchPortal.Api
dotnet user-secrets set "GeminiEmbedding:ApiKey" "<your-gemini-key>" --project src/UiucResearchPortal.Api
```

Replace `<your-groq-key>` and `<your-gemini-key>` with the real keys from step 2
(including the quotes around them, on both Ubuntu and Windows).

You do **not** need to set `ConnectionStrings:Postgres` — `appsettings.json` already
has `Host=localhost;Port=5432;Database=uiuc_research_portal;Username=postgres;Password=postgres`,
which matches the Docker container from step 1 exactly. Only set it yourself if you
changed the Docker command above or are pointing at a hosted Postgres instead.

### 4. Apply database migrations

This creates the tables (including the `vector(768)` column) in the empty Postgres
container.

```bash
dotnet tool install --global dotnet-ef   # once, if you don't already have it
dotnet ef database update --project src/UiucResearchPortal.Infrastructure --startup-project src/UiucResearchPortal.Api
```

**Windows note:** if `dotnet ef` isn't recognized right after the tool install,
open a **new** PowerShell window — the install adds `dotnet-ef` to a PATH entry
(`%USERPROFILE%\.dotnet\tools`) that's only picked up by new shells.

**Ubuntu note:** likewise, if you get `command not found: dotnet-ef` and
`~/.dotnet/tools` isn't on your `PATH` yet, run
`export PATH="$PATH:$HOME/.dotnet/tools"` (add that line to `~/.bashrc` to make it
permanent) and retry.

### 5. Run the ingestion (seeds the database)

```bash
dotnet run --project src/UiucResearchPortal.Ingestion
```

This fetches recent UIUC papers from OpenAlex, imports `data/illinois-researchers.csv`,
matches authors by name, computes embeddings via the Gemini key from step 3, and
writes everything to the Postgres container from step 1. Takes a few minutes depending
on OpenAlex response times and embedding API rate limits — it logs progress to the
console as it goes, so it's not stuck if you don't see new lines for a few seconds.

Run this from the `backend/` directory (same place as step 3 and 4); if your shell is
somewhere else, `cd` back to `backend/` first.

### 6. Run the API

```bash
dotnet run --project src/UiucResearchPortal.Api
```

API runs at `http://localhost:5070` (Swagger UI at http://localhost:5070/swagger —
useful for poking at endpoints directly without the frontend). Leave this running in
its own terminal window/tab; open a new one for the frontend in step 7.

### 7. Run the frontend

Open a **second** terminal (leave the API running in the first one), then:

```bash
cd frontend
npm install
npm start
```

`npm install` only needs to run once (or again after pulling changes that touch
`package.json`). First install can take a minute or two.

App runs at http://localhost:4200 and calls the backend at
`http://localhost:5070/api` (configured in `src/environments/environment.ts`). Open
that URL in a browser to see the portal and chat widget.

### Everyday restart (after the first full setup above)

Once steps 0–5 have been done once, restarting later only needs:

```bash
docker start uiuc-pg          # if you removed/stopped the container
cd backend && dotnet run --project src/UiucResearchPortal.Api   # terminal 1
cd frontend && npm start                                        # terminal 2
```

(API keys set via `dotnet user-secrets` persist across restarts — no need to set them
again unless you switch machines or delete the secrets store.)

## Running Tests

```bash
cd backend
dotnet test                              # unit tests only is the default; one test hits the real OpenAlex API
dotnet test --filter "Category=Integration"   # just the OpenAlex live-API check
```

Works identically on Ubuntu and Windows.

## Troubleshooting

- **`docker: command not found` / Docker Desktop not starting (Windows)**: Docker
  Desktop must be running (check the system tray whale icon) before `docker run`
  works. On Windows, Docker Desktop also needs WSL 2 enabled — the installer does this
  automatically, but if it didn't, run `wsl --install` in an elevated PowerShell and
  reboot, then reopen Docker Desktop.
- **`Host=localhost;Port=5432;...` connection refused**: the Postgres container isn't
  running — check with `docker ps --filter name=uiuc-pg`. If it's not listed, start it
  with `docker start uiuc-pg`, or re-run the `docker run ...` command from step 1 if
  you removed it.
- **Port 5432 already in use**: you likely have another Postgres instance (local
  install or another container) already bound to that port. Either stop it, or change
  the `-p` mapping in step 1 (e.g. `-p 5433:5432`) and update
  `ConnectionStrings:Postgres` in step 3 to use port `5433` instead.
- **Port 5070 or 4200 already in use**: another process is using that port. For the
  API, change `applicationUrl` in
  `backend/src/UiucResearchPortal.Api/Properties/launchSettings.json`. For the
  frontend, run `npm start -- --port 4300` instead and update
  `src/environments/environment.ts`'s `apiBaseUrl` to match if you also changed the
  API's port.
- **CORS error in the browser console**: the frontend's origin must be listed in the
  API's `Cors:AllowedOrigins` (`backend/src/UiucResearchPortal.Api/appsettings.json`).
  It already includes `http://localhost:4200` by default — only an issue if you changed
  the frontend's port.
- **Ingestion runs but the chat gives "not enough information" for everything**: check
  the ingestion console output for errors (e.g. a bad/expired API key, or an OpenAlex
  rate limit) — it logs failures per batch rather than silently skipping them.

## Key Design Decisions

See [`docs/decisions.md`](docs/decisions.md) for the live-updated log of
architectural decisions and trade-offs made during development.

## License

Internal technical assessment project.
