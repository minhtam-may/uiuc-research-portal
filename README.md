# UIUC Research Portal & Domain-Specific LLM Assistant

Single-page institutional research portal for the University of Illinois
Urbana-Champaign (UIUC), paired with a domain-specific LLM assistant that
answers questions about recent UIUC research, publications, and faculty
expertise.

Built for the "Institutional Research Portal and Domain-Specific LLM
Assistant" technical assessment.

## Status

🚧 In progress — scaffolding stage.

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
- **Embeddings**: Google Gemini `text-embedding-004` (free tier)
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
- A free Postgres database with the `pgvector` extension (e.g. [Supabase](https://supabase.com) or [Neon](https://neon.tech) free tier)
- Free API keys: [Groq](https://console.groq.com) (LLM) and [Google AI Studio](https://aistudio.google.com) (Gemini embeddings)

### Backend

```bash
cd backend
dotnet user-secrets init --project src/UiucResearchPortal.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "<your-postgres-connection-string>" --project src/UiucResearchPortal.Api
dotnet user-secrets set "Groq:ApiKey" "<your-groq-key>" --project src/UiucResearchPortal.Api
dotnet user-secrets set "GeminiEmbedding:ApiKey" "<your-gemini-key>" --project src/UiucResearchPortal.Api

dotnet build
dotnet run --project src/UiucResearchPortal.Api
```

API runs at `https://localhost:5080` (Swagger UI at `/swagger` in Development).

_(DB migrations and the ingestion step will be documented here once implemented.)_

### Frontend

```bash
cd frontend
npm install
npm start
```

App runs at `http://localhost:4200` and calls the backend at the URL configured in
`src/environments/environment.ts`.

## Key Design Decisions

See [`docs/decisions.md`](docs/decisions.md) for the live-updated log of
architectural decisions and trade-offs made during development.

## License

Internal technical assessment project.
