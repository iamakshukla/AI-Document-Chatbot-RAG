# QMS ChatBot

A Retrieval-Augmented Generation (RAG) chatbot that answers questions from your own PDF documents.
Built with **ASP.NET Core 9** and a locally running **Ollama** instance, so no data leaves your machine.

## How it works

1. **Upload** a PDF: text is extracted (PdfPig) and split into overlapping chunks (500 chars, 100 overlap).
2. Each chunk is turned into an embedding with `nomic-embed-text` and kept in an in-memory vector store.
3. **Ask** a question: it is embedded, the top 3 chunks are found by cosine similarity, and `llama3.2:1b` answers using only those chunks.

## Tech stack

- ASP.NET Core 9 Web API
- Ollama (`nomic-embed-text` for embeddings, `llama3.2:1b` for answers)
- PdfPig for PDF text extraction
- Plain HTML/JS chat UI served from `wwwroot`

## Getting started

Prerequisites: [.NET 9 SDK](https://dotnet.microsoft.com/download) and [Ollama](https://ollama.com/download).

```bash
ollama pull nomic-embed-text
ollama pull llama3.2:1b

cd QMS.ChatBot.Api
dotnet run
```

Open http://localhost:5044 for the chat UI.

## API

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/chat/test` | Health check |
| POST | `/api/chat/upload` | Upload a PDF (`multipart/form-data`, field `file`) |
| POST | `/api/chat/search` | Return the top 3 matching chunks: `{ "question": "..." }` |
| POST | `/api/chat/ask` | RAG answer plus timing breakdown: `{ "question": "..." }` |

If Ollama is not running, the API returns `503` with a clear message instead of crashing.

## Notes

- The vector store is in memory, so uploaded documents must be re-uploaded after a restart.
- Uploaded PDFs are saved to `QMS.ChatBot.Api/Uploads/` (git-ignored).
