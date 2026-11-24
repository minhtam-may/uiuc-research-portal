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

## How to Run

### Prerequisites

- .NET 8 SDK
- Node.js 20+ / npm
- Docker (for a local Postgres + pgvector instance — see below)
- Free API keys: Groq (LLM) and Google Gemini (embeddings) — see **Getting free API keys** below

No step here costs money. Every service used has a free tier that is enough for this
project's scale (~1,000 researchers, ~10k–20k papers).

### 1. Start Postgres with pgvector (Docker, free, local)

```bash
docker run -d --name uiuc-pg \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=uiuc_research_portal \
  -p 5432:5432 \
  pgvector/pgvector:pg16
```

This pulls the official `pgvector/pgvector` image (Postgres 16 with the `vector`
extension preinstalled) and starts it on `localhost:5432`. No account or sign-up
needed. To stop/remove it later: `docker rm -f uiuc-pg`.

_(Alternative: a hosted free-tier Postgres such as [Supabase](https://supabase.com) or
[Neon](https://neon.tech) also works — both support `pgvector`. Just use that
connection string instead in step 3 below.)_

### 2. Getting free API keys

**Groq (LLM — generates the chat answers):**
1. Go to https://console.groq.com and sign in (Google/GitHub login works, no credit card).
2. Open **API Keys** in the left sidebar → **Create API Key**.
3. Give it any name, copy the key (starts with `gsk_...`). You won't be able to see it again.
4. Free tier gives a generous daily request quota — enough for this project's testing/demo.

**Google Gemini (embeddings — powers semantic search over papers):**
1. Go to https://aistudio.google.com/apikey and sign in with a Google account.
2. Click **Create API key** → choose or create a Google Cloud project when prompted (no
   billing required for the free tier).
3. Copy the key (starts with `AIza...`).

Keep both keys private. **Never commit them to the repo** — the steps below store them
locally via `dotnet user-secrets`, which keeps them out of source control entirely, so
anyone who clones this repo (today or in a year) uses their *own* free key instead of a
shared one that could get rate-limited or revoked.

### 3. Configure the backend

```bash
cd backend
dotnet user-secrets init --project src/UiucResearchPortal.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=uiuc_research_portal;Username=postgres;Password=postgres" --project src/UiucResearchPortal.Api
dotnet user-secrets set "Groq:ApiKey" "<your-groq-key>" --project src/UiucResearchPortal.Api
dotnet user-secrets set "GeminiEmbedding:ApiKey" "<your-gemini-key>" --project src/UiucResearchPortal.Api
```

(Skip the `ConnectionStrings:Postgres` line if you're using the Docker command above
as-is — it already matches `appsettings.json`'s default.)

### 4. Apply database migrations

```bash
dotnet tool install --global dotnet-ef   # once, if you don't have it
dotnet ef database update --project src/UiucResearchPortal.Infrastructure --startup-project src/UiucResearchPortal.Api
```

### 5. Run the ingestion (seeds the database)

```bash
dotnet run --project src/UiucResearchPortal.Ingestion
```

This fetches recent UIUC papers from OpenAlex, imports `data/illinois-researchers.csv`,
matches authors by name, computes embeddings, and writes everything to Postgres.
Takes a few minutes depending on OpenAlex response times and embedding API rate limits.

### 6. Run the API

```bash
dotnet run --project src/UiucResearchPortal.Api
```

API runs at `http://localhost:5070` (Swagger UI at `/swagger` in Development).

### Frontend

```bash
cd frontend
npm install
npm start
```

App runs at `http://localhost:4200` and calls the backend at the URL configured in
`src/environments/environment.ts`.

## Running Tests

```bash
cd backend
dotnet test                              # unit tests only is the default; one test hits the real OpenAlex API
dotnet test --filter "Category=Integration"   # just the OpenAlex live-API check
```

## Key Design Decisions

See [`docs/decisions.md`](docs/decisions.md) for the live-updated log of
architectural decisions and trade-offs made during development.

## Quick demo via GitHub Codespaces (no external hosting account needed)

For a fast "just let me see it running" check without signing up for any
hosting provider, this repo has a [`.devcontainer/devcontainer.json`](.devcontainer/devcontainer.json)
so the whole stack — Postgres, .NET API, Angular frontend — runs inside a
single free GitHub Codespace:

1. On the repo's GitHub page: **Code** → **Codespaces** tab → **Create
   codespace on main**. (Free personal GitHub accounts get 60 core-hours/month
   free — plenty for a demo session.)
2. Wait for the container to build (installs .NET 8, Node 20, and starts
   Postgres+pgvector via `docker-compose.yml` automatically on first boot).
3. In the Codespace's terminal, set your API keys and run the ingestion once
   (same commands as **How to Run** below, steps 2–5) — Codespaces gives you
   a normal Linux shell, so every command in this README works as-is.
4. Run the API (`dotnet run --project backend/src/UiucResearchPortal.Api`)
   and the frontend (`npm start` inside `frontend/`). Codespaces auto-detects
   the forwarded ports (4200, 5070) and offers a "Open in Browser" popup, or
   check the **Ports** tab.
5. When you're done, stop or delete the Codespace from the Codespaces list
   (https://github.com/codespaces) so it doesn't eat into the free monthly
   hours while idle.

This is for a quick look during review, not a permanently-online deployment —
nothing here needs a long-lived public URL, and GitHub Codespaces keeps
everything (code, secrets typed into the terminal, Postgres data) inside your
own GitHub account rather than a third-party host.

## License

Internal technical assessment project.
