# Key Design Decisions Log

Ghi lại quyết định kỹ thuật ngay khi chọn, để lúc điền form không phải nhớ lại.

| Date | Decision | Reason | Alternatives considered |
|------|----------|--------|--------------------------|
| TBD | Monorepo: `frontend/` (Angular 17) + `backend/` (.NET 8) in one repo | Đề thi quy mô nhỏ, 1 README, 1 link repo để nộp form | 2 repos riêng — tăng overhead quản lý không cần thiết |
| TBD | Data store: Postgres (Supabase/Neon free tier) + pgvector extension | Free, đủ cho ~20k papers + 1000 researchers, vector search nằm cùng DB quan hệ, không cần thêm vector DB riêng | SQLite (không có vector search tốt), Pinecone/Weaviate free tier (thêm 1 service ngoài, phức tạp hơn) |
| TBD | LLM: Groq API free tier (Llama 3.1/3.3) | Free, nhanh, đủ tốt cho RAG trả lời câu hỏi domain-specific | Google Gemini free tier (backup option), Ollama local (phụ thuộc máy, không ổn định để demo) |
| TBD | Embedding: Gemini `text-embedding-004` free tier | Free, chất lượng tốt cho semantic search tiếng Anh academic text | Tự host ONNX model trong .NET (phức tạp hơn, không cần thiết cho scope đề) |
| TBD | Data ingestion: console app .NET riêng (`backend/src/Ingestion`), chạy 1 lần để seed DB | Tách biệt concern: ingest là batch job, không nên nằm trong API runtime | Hosted background service trong API — tăng độ phức tạp runtime không cần thiết |
| TBD | Author matching: match tên (fuzzy) giữa CSV researchers và OpenAlex authorships | OpenAlex không có email UIUC để match chính xác, phải dùng display_name + institution filter | Bỏ qua matching, hiển thị riêng 2 nguồn — làm mất liên kết "faculty nào có paper nào" theo yêu cầu đề |
| TBD | `RagRetriever.FindRelevantResearchersAsync` dùng lexical keyword match (ILIKE) thay vì embedding similarity cho researchers | Đơn giản, đủ cho scope đề thi (1000 researchers, keyword list có sẵn sẵn rõ ràng); tránh phải embed lại 1000 researcher profiles | Embed từng researcher keyword-blob và tính cosine distance giống papers — cân nhắc nếu câu hỏi tự do (semantic) không khớp tốt bằng keyword match |

## Ghi chú vấn đề gặp phải (Problem Solving) — cập nhật dần

- **Model names đã lỗi thời (test với key thật)**: sau khi có key Groq + Gemini thật, test trực
  tiếp bằng `curl` phát hiện cả 2 default model đã chọn ban đầu không còn khả dụng:
  - Gemini: `text-embedding-004` → 404. Model embedding hiện tại trên free tier là
    `gemini-embedding-001`, output mặc định 3072 chiều. Dùng tham số `outputDimensionality: 768`
    trong request để khớp đúng cột `vector(768)` đã migrate — verified qua curl (trả về đúng 768
    giá trị). pgvector's cosine distance (`<=>`) tự chia theo L2 norm nên không cần tự normalize
    thủ công dù Google doc gợi ý nên normalize khi dùng dimension < 3072 (xác nhận qua pgvector
    GitHub docs).
  - Groq: `llama-3.3-70b-versatile` → "model_not_found" (đã bị gỡ khỏi lineup). Gọi
    `GET /v1/models` với key thật để lấy danh sách hiện có, chọn `openai/gpt-oss-120b` (chat
    model, free tier) — verified trả lời đúng qua curl.
  - Bài học: không hardcode tên model LLM/embedding provider third-party làm default lâu dài —
    lineup các provider free tier thay đổi thường xuyên; nên luôn verify bằng 1 call thật trước
    khi tin tưởng, và cấu hình qua appsettings để đổi được không cần build lại.
- **Bug: DOI URL bị double-prefix trong citation**: test `/api/chat` thật phát hiện citation URL
  dạng `https://doi.org/https://doi.org/10.xxx`. OpenAlex's `doi` field trả về sẵn URL đầy đủ
  (`https://doi.org/10.xxx`), không phải chỉ phần số DOI — `ResearchChatService.AnswerAsync` lại
  tự prepend thêm `https://doi.org/` một lần nữa. Sửa: dùng trực tiếp `p.Paper.Doi` không prepend.
- **Deploy lên hosting ngoài (Render/Vercel/Neon → GitHub Codespaces → Railway): thử rồi bỏ**.
  Đề chỉ yêu cầu chạy được, không yêu cầu 1 URL public cố định, và mọi hướng free-hosting thử qua
  đều có đánh đổi không đáng (Render/Vercel/Neon: thêm 3 tài khoản ngoài GitHub; Codespaces:
  không có URL public cố định, chỉ port-forward tạm trong session; Railway: trial plan không
  tương thích `Dockerfile` đã viết — CLI tự chọn builder Railpack/Nixpacks thay vì Dockerfile ở
  root khác `backend/`, build fail "could not determine how to build the app"). Quyết định giữ
  scope đúng đề bài: chạy local qua Docker Compose (`pgvector/pgvector:pg16`) + `dotnet run` +
  `npm start`, đã verify thật nhiều lần (xem các mục bug/fix phía trên). `backend/Dockerfile` vẫn
  được build+run thật ít nhất 1 lần (verified) nên vẫn là tài liệu đúng nếu sau này cần
  containerize, nhưng không còn nằm trong hướng dẫn "How to Run" chính.
- **NuGet version conflict (pgvector + EF Core 8)**: `Pgvector.EntityFrameworkCore` bản mới nhất (0.3.0) kéo theo
  `Npgsql.EntityFrameworkCore.PostgreSQL 9.x`, không tương thích với EF Core 8 mà project target (.NET 8).
  Giải pháp: pin `Pgvector.EntityFrameworkCore` về `0.2.2` (dòng tương thích EF Core 8/Npgsql 8), và pin
  `Pgvector` về `0.3.1` (version tối thiểu mà `Pgvector.EntityFrameworkCore 0.2.2` yêu cầu) để tránh lỗi
  NU1605 "package downgrade". Nguồn: thử restore trực tiếp qua `dotnet add package`, đọc lỗi NU1605 chỉ ra
  chain dependency cụ thể.
- **OpenAlex filter cú pháp range sai**: dùng `publication_date:2024-01-01..` (range mở) gây lỗi
  "Invalid query parameters" — OpenAlex không hỗ trợ cú pháp `..` mở một đầu kiểu đó cho
  `publication_date`. Test trực tiếp qua `curl` phát hiện field đúng cần dùng là
  `from_publication_date:2024-01-01` (so sánh >=), không phải `publication_date` kèm range.
  Sửa trong `OpenAlexClient.BuildUrl`. Đã verify qua `curl` thật: filter
  `institutions.id:I157725225,from_publication_date:2024-01-01,type:article` trả về count=20814,
  đúng trong khoảng 10k–20k mà đề yêu cầu.
- **Lưu ý chất lượng dữ liệu OpenAlex**: một số affiliation trong `authorships[].institutions` có vẻ
  gán nhầm (ví dụ bài báo tiếng Nga hoàn toàn không liên quan UIUC vẫn có 1 author gắn institution
  "University of Illinois Urbana-Champaign"), đây là vấn đề chất lượng dữ liệu nguồn đã biết của
  OpenAlex, không phải lỗi filter. Mitigation: `IngestionRunner` chỉ giữ paper nếu có ít nhất 1
  authorship có `IsUiucAffiliated = true` sau khi check institution id ở từng authorship (không
  chỉ dựa vào việc work match filter cấp work), và việc match tên với CSV 1000 giảng viên UIUC là
  một lớp lọc chéo thêm: nếu không match được tên nào, paper vẫn được lưu kèm
  `AuthorDisplayNames` để hiển thị nhưng không liên kết sai vào 1 giảng viên cụ thể.
- **Rủi ro EF Core/Npgsql không dịch được nested LINQ `Any`**: `RagRetriever.FindRelevantResearchersAsync`
  ban đầu viết `terms.Any(t => r.Keywords.Any(k => EF.Functions.ILike(k, $"%{t}%")))` — nested Any
  trộn 1 mảng C# trong memory (`terms`, từ closure) với 1 cột DB (`r.Keywords`) bên trong 1 lambda.
  Theo tài liệu Npgsql (npgsql.org/efcore/mapping/array.html), pattern được hỗ trợ chính thức là
  dạng đơn `array.Any(s => EF.Functions.ILike(string, s))` dịch sang `ILIKE ANY`, không rõ có hỗ
  trợ lồng 2 cấp Any như trên hay sẽ throw "could not be translated" lúc runtime (chưa test được
  trực tiếp với DB thật tại thời điểm viết, do máy chưa có Docker/Postgres). Để tránh rủi ro, đổi
  sang: build predicate riêng cho mỗi term (`Keywords.Any(k => ILIKE(k, pattern))`, đúng pattern
  tài liệu xác nhận), rồi OR các predicate lại bằng `Expression.OrElse` + đổi parameter cho khớp,
  thay vì nested Any 2 nguồn khác nhau. Sẽ verify lại bằng integration test thật khi có Postgres.
- **Model names đã lỗi thời (test với key thật)**: sau khi có key Groq + Gemini thật, test trực
  tiếp bằng `curl` phát hiện cả 2 default model đã chọn ban đầu không còn khả dụng:
  - Gemini: `text-embedding-004` → 404. Model embedding hiện tại trên free tier là
    `gemini-embedding-001`, output mặc định 3072 chiều. Dùng tham số `outputDimensionality: 768`
    trong request để khớp đúng cột `vector(768)` đã migrate — verified qua curl (trả về đúng 768
    giá trị). pgvector's cosine distance (`<=>`) tự chia theo L2 norm nên không cần tự normalize
    thủ công dù Google doc gợi ý nên normalize khi dùng dimension < 3072 (xác nhận qua pgvector
    GitHub docs).
  - Groq: `llama-3.3-70b-versatile` → "model_not_found" (đã bị gỡ khỏi lineup). Gọi
    `GET /v1/models` với key thật để lấy danh sách hiện có, chọn `openai/gpt-oss-120b` (chat
    model, free tier) — verified trả lời đúng qua curl.
  - Bài học: không hardcode tên model LLM/embedding provider third-party làm default lâu dài —
    lineup các provider free tier thay đổi thường xuyên; nên luôn verify bằng 1 call thật trước
    khi tin tưởng, và cấu hình qua appsettings để đổi được không cần build lại.
- **Bug thật phát hiện ở lần chạy ingestion thật đầu tiên**: `DbUpdateException` duplicate key
  trên `IX_researchers_Email`. Nguyên nhân: dataset CSV thật có 1 email trùng
  (`c-cheng@illinois.edu` xuất hiện 2 lần, verified bằng script Python đếm trực tiếp trên CSV).
  `ImportResearchersAsync` chỉ build `existingByEmail` từ DB hiện có trước khi loop; dòng CSV thứ
  2 trùng email trong cùng lần chạy không được nhận diện là "đã thấy" (vì chưa `SaveChanges`), nên
  cả 2 dòng cùng `_db.Researchers.Add(...)` → vi phạm unique constraint khi save. Sửa: thêm
  researcher mới vào `existingByEmail` ngay sau khi `Add`, để dòng trùng tiếp theo trong cùng lần
  chạy được coi là update thay vì insert. Đây là lý do tại sao "test với dữ liệu thật" (không chỉ
  unit test với dữ liệu giả định sạch) là bắt buộc trước khi tin một pipeline ingestion.
- **Bug: DOI URL bị double-prefix trong citation**: test `/api/chat` thật phát hiện citation URL
  dạng `https://doi.org/https://doi.org/10.xxx`. OpenAlex's `doi` field trả về sẵn URL đầy đủ
  (`https://doi.org/10.xxx`), không phải chỉ phần số DOI — `ResearchChatService.AnswerAsync` lại
  tự prepend thêm `https://doi.org/` một lần nữa. Sửa: dùng trực tiếp `p.Paper.Doi` không prepend.
- (sẽ cập nhật tiếp khi gặp vấn đề thực tế trong lúc build: rate limit OpenAlex, CORS giữa Angular
  và .NET, v.v.)

## Time log (để trả lời "Estimated Time Spent")

| Date | Hours | Việc đã làm |
|------|-------|-------------|
| Day 1 | ~2h | Khởi tạo repo, scaffold Angular 17 frontend (standalone components, Material, portal + chat features), scaffold .NET 8 backend (Domain/Application/Infrastructure/Api/Ingestion, EF Core + pgvector, Groq LLM client stub, Gemini embedding client stub, 4 API controllers). Build sạch cả 2 phía. |
| Day 1 | ~2h | OpenAlex client thật (cursor paging, decode abstract_inverted_index, verify qua curl + fix filter cú pháp `from_publication_date`), CSV importer + Postgres-array parser (test với 1000 dòng CSV thật), author name matcher (fuzzy match CSV ↔ OpenAlex), Ingestion orchestrator, EF Core migration (pgvector 768-dim). 11 automated tests (unit + 1 integration gọi OpenAlex thật), tất cả pass. |
