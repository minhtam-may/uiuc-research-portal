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

- **NuGet version conflict (pgvector + EF Core 8)**: `Pgvector.EntityFrameworkCore` bản mới nhất (0.3.0) kéo theo
  `Npgsql.EntityFrameworkCore.PostgreSQL 9.x`, không tương thích với EF Core 8 mà project target (.NET 8).
  Giải pháp: pin `Pgvector.EntityFrameworkCore` về `0.2.2` (dòng tương thích EF Core 8/Npgsql 8), và pin
  `Pgvector` về `0.3.1` (version tối thiểu mà `Pgvector.EntityFrameworkCore 0.2.2` yêu cầu) để tránh lỗi
  NU1605 "package downgrade". Nguồn: thử restore trực tiếp qua `dotnet add package`, đọc lỗi NU1605 chỉ ra
  chain dependency cụ thể.
- (sẽ cập nhật tiếp khi gặp vấn đề thực tế trong lúc build: OpenAlex pagination/rate limit, decode
  abstract_inverted_index, fuzzy name matching edge cases, CORS giữa Angular và .NET, v.v.)

## Time log (để trả lời "Estimated Time Spent")

| Date | Hours | Việc đã làm |
|------|-------|-------------|
| Day 1 | ~2h | Khởi tạo repo, scaffold Angular 17 frontend (standalone components, Material, portal + chat features), scaffold .NET 8 backend (Domain/Application/Infrastructure/Api/Ingestion, EF Core + pgvector, Groq LLM client stub, Gemini embedding client stub, 4 API controllers). Build sạch cả 2 phía. |
