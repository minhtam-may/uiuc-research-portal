# Key Design Decisions Log

Logged as each decision was made, so the assessment form's design-decisions
field doesn't rely on memory later.

| Date | Decision | Reason | Alternatives considered |
|------|----------|--------|--------------------------|
| TBD | Monorepo: `frontend/` (Angular 17) + `backend/` (.NET 8) in one repo | Small-scope assessment, one README, one repo link to submit on the form | Two separate repos — unnecessary management overhead |
| TBD | Data store: Postgres (Supabase/Neon free tier) + pgvector extension | Free, enough for ~20k papers + 1,000 researchers; vector search lives in the same relational DB, no separate vector DB needed | SQLite (no good vector search), Pinecone/Weaviate free tier (an extra external service, more complexity) |
| TBD | LLM: Groq API free tier (Llama 3.1/3.3) | Free, fast, good enough for RAG answers to domain-specific questions | Google Gemini free tier (backup option), local Ollama (depends on the machine, not stable enough for a demo) |
| TBD | Embedding: Gemini `text-embedding-004` free tier | Free, good quality for semantic search over English academic text | Self-hosting an ONNX model in .NET (more complex, not needed for this assessment's scope) |
| TBD | Data ingestion: a separate .NET console app (`backend/src/Ingestion`), run once to seed the DB | Separation of concerns: ingestion is a batch job and shouldn't live in the API's runtime | A hosted background service inside the API — unnecessary runtime complexity |
| TBD | Author matching: fuzzy name matching between CSV researchers and OpenAlex authorships | OpenAlex has no UIUC email to match on exactly, so matching has to go through display_name + an institution filter | Skip matching and show the two sources separately — would lose the "which faculty member has which paper" link the assessment asks for |
| TBD | `RagRetriever.FindRelevantResearchersAsync` uses lexical keyword matching (ILIKE) instead of embedding similarity for researchers | Simple, and enough for this assessment's scope (1,000 researchers with a ready-made, well-defined keyword list); avoids re-embedding 1,000 researcher profiles | Embed each researcher's keyword blob and compute cosine distance the same way as papers — worth revisiting if free-form (semantic) questions don't match well enough against keywords |

## Problem-solving notes — updated as issues came up

- **Deploy to external hosting (Render/Vercel/Neon → GitHub Codespaces →
  Railway): tried, then dropped.** The assessment only requires the project to
  run, not a fixed public URL, and every free-hosting path tried came with a
  trade-off that wasn't worth it (Render/Vercel/Neon: three extra accounts
  beyond GitHub; Codespaces: no fixed public URL, only a temporary
  per-session port forward; Railway: the trial plan didn't cooperate with the
  `Dockerfile` already written — its CLI picked the Railpack/Nixpacks builder
  instead of the Dockerfile at `backend/`'s root, and the build failed with
  "could not determine how to build the app"). Decided to keep the scope
  matching the assessment: run locally via Docker (`pgvector/pgvector:pg16`)
  + `dotnet run` + `npm start`, verified for real multiple times (see the
  bug/fix notes above and below). `backend/Dockerfile` was still built and run
  for real at least once (verified), so it remains accurate documentation if
  containerizing is ever needed later, but it's no longer part of the main
  "How to Run" instructions.
- **NuGet version conflict (pgvector + EF Core 8)**: the latest
  `Pgvector.EntityFrameworkCore` (0.3.0) pulls in `Npgsql.EntityFrameworkCore.PostgreSQL 9.x`,
  which isn't compatible with EF Core 8 (what this project targets on .NET 8).
  Fix: pinned `Pgvector.EntityFrameworkCore` to `0.2.2` (the line compatible
  with EF Core 8/Npgsql 8), and pinned `Pgvector` to `0.3.1` (the minimum
  version `Pgvector.EntityFrameworkCore 0.2.2` requires) to avoid an NU1605
  "package downgrade" error. Found by restoring directly via
  `dotnet add package` and reading the NU1605 error, which pointed at the
  exact dependency chain.
- **Wrong OpenAlex range filter syntax**: using `publication_date:2024-01-01..`
  (an open-ended range) caused an "Invalid query parameters" error — OpenAlex
  doesn't support that kind of one-sided `..` range syntax for
  `publication_date`. Testing directly with `curl` showed the correct field is
  `from_publication_date:2024-01-01` (a >= comparison), not `publication_date`
  with a range. Fixed in `OpenAlexClient.BuildUrl`. Verified with a real
  `curl` call: the filter
  `institutions.id:I157725225,from_publication_date:2024-01-01,type:article`
  returns count=20814, correctly within the 10k–20k range the assessment asks
  for.
- **OpenAlex data quality note**: some affiliations in
  `authorships[].institutions` look mis-tagged (e.g. a paper entirely in
  Russian, unrelated to UIUC, still has one author tagged with institution
  "University of Illinois Urbana-Champaign") — this is a known source data
  quality issue with OpenAlex, not a filter bug. Mitigation: `IngestionRunner`
  only keeps a paper if at least one authorship has
  `IsUiucAffiliated = true` after checking the institution id on each
  individual authorship (not just relying on the work matching the filter at
  the work level), and matching names against the CSV of 1,000 UIUC faculty
  acts as an extra cross-check: if no name matches, the paper is still stored
  with `AuthorDisplayNames` for display, but without being wrongly linked to a
  specific faculty member.
- **Risk: EF Core/Npgsql not translating a nested LINQ `Any`**:
  `RagRetriever.FindRelevantResearchersAsync` originally wrote
  `terms.Any(t => r.Keywords.Any(k => EF.Functions.ILike(k, $"%{t}%")))` —
  a nested `Any` mixing an in-memory C# array (`terms`, from the closure) with
  a DB column (`r.Keywords`) inside one lambda. Per the Npgsql docs
  (npgsql.org/efcore/mapping/array.html), the officially supported pattern is
  the single-level `array.Any(s => EF.Functions.ILike(string, s))`, which
  translates to `ILIKE ANY`; it wasn't clear whether a 2-level nested `Any`
  like the above would also be supported or would throw "could not be
  translated" at runtime (untested against a real DB at the time of writing,
  since the machine didn't have Docker/Postgres yet). To avoid the risk,
  changed it to: build a separate predicate per term
  (`Keywords.Any(k => ILIKE(k, pattern))`, the exact pattern the docs
  confirm), then OR the predicates together via `Expression.OrElse` with the
  parameter rebound to match, instead of nesting two different sources inside
  one `Any`. Confirmed later against a real Postgres instance via integration
  testing.
- **Model names had gone stale (caught once testing with real keys)**: once
  real Groq and Gemini keys were available, testing directly with `curl`
  found both originally-chosen default models were no longer available:
  - Gemini: `text-embedding-004` → 404. The current embedding model on the
    free tier is `gemini-embedding-001`, which defaults to 3072 dimensions.
    Used the `outputDimensionality: 768` request parameter to match the
    migrated `vector(768)` column — verified via curl (returned exactly 768
    values). pgvector's cosine distance (`<=>`) already divides by the L2
    norm, so no manual normalization was needed, even though Google's docs
    suggest normalizing when using a dimension below 3072 (confirmed against
    pgvector's GitHub docs).
  - Groq: `llama-3.3-70b-versatile` → "model_not_found" (removed from the
    lineup). Called `GET /v1/models` with a real key to get the current list,
    and picked `openai/gpt-oss-120b` (chat model, free tier) — verified it
    answers correctly via curl.
  - Takeaway: don't hardcode a third-party LLM/embedding provider's model
    name as a long-term default — free-tier provider lineups change often.
    Always verify with one real call before trusting a model name, and keep
    it configurable via appsettings so it can change without a rebuild.
- **Real bug found on the first real ingestion run**: a `DbUpdateException`
  on a duplicate key for `IX_researchers_Email`. Cause: the real CSV dataset
  has one duplicate email (`c-cheng@illinois.edu` appears twice, confirmed by
  a Python script counting it directly in the CSV). `ImportResearchersAsync`
  only built `existingByEmail` from what was already in the DB before the
  loop started; the second CSV row with the same email, within the same run,
  wasn't recognized as "already seen" (since `SaveChanges` hadn't run yet),
  so both rows got added via `_db.Researchers.Add(...)` → violating the
  unique constraint on save. Fix: add the new researcher to
  `existingByEmail` right after `Add`, so a later duplicate row in the same
  run is treated as an update instead of an insert. This is why testing
  against real data (not just unit tests against clean, assumed-correct
  data) is mandatory before trusting an ingestion pipeline.
- **Bug: DOI URL double-prefixed in citations**: testing `/api/chat` for
  real surfaced a citation URL shaped like
  `https://doi.org/https://doi.org/10.xxx`. OpenAlex's `doi` field already
  returns the full URL (`https://doi.org/10.xxx`), not just the bare DOI
  number — `ResearchChatService.AnswerAsync` was prepending
  `https://doi.org/` to it a second time. Fix: use `p.Paper.Doi` directly,
  without prepending anything.
- (will keep updating as real issues come up during the build: OpenAlex rate
  limits, CORS between Angular and .NET, etc.)

## Time log (for "Estimated Time Spent")

| Date | Hours | Work done |
|------|-------|-------------|
| Day 1 | ~2h | Initialized the repo, scaffolded the Angular 17 frontend (standalone components, Material, portal + chat features), scaffolded the .NET 8 backend (Domain/Application/Infrastructure/Api/Ingestion, EF Core + pgvector, Groq LLM client stub, Gemini embedding client stub, 4 API controllers). Clean build on both sides. |
| Day 1 | ~2h | Real OpenAlex client (cursor paging, decoding `abstract_inverted_index`, verified via curl + fixed the `from_publication_date` filter syntax), CSV importer + Postgres array parser (tested against the real 1,000-row CSV), author name matcher (fuzzy match CSV ↔ OpenAlex), ingestion orchestrator, EF Core migration (768-dim pgvector). 11 automated tests (unit + 1 integration test hitting the real OpenAlex API), all passing. |
