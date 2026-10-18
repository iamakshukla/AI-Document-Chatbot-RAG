
using Microsoft.AspNetCore.Mvc;
using QMS.Chatbot.Api.Services;
using QMS.Chatbot.Api.Models;
using System.Diagnostics;

namespace QMS.Chatbot.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly PdfService _pdfService;
        private readonly TextChunkService _textChunkService;
        private readonly EmbeddingService _embeddingService;
        private readonly VectorStoreService _vectorStore;
        private readonly ChatGenerationService _chatGenerationService;

        // Constructor Dependency Injection
        public ChatController(
            PdfService pdfService,
            TextChunkService textChunkService,
            EmbeddingService embeddingService,
            VectorStoreService vectorStore,
            ChatGenerationService chatGenerationService)
        {
            _pdfService = pdfService;
            _textChunkService = textChunkService;
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _chatGenerationService = chatGenerationService;
        }

        // API 1: Test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "QMS Chatbot API is running."
            });
        }

        // API 2: Upload PDF
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please select a PDF file.");
            }

            if (Path.GetExtension(file.FileName)
                .ToLowerInvariant() != ".pdf")
            {
                return BadRequest("Only PDF files are allowed.");
            }

            // Step 1: Save PDF
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "Uploads");

            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}.pdf";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(
                filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Step 2: Extract PDF text
            string extractedText =
                _pdfService.ExtractText(filePath);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return BadRequest(
                    "No readable text found in PDF.");
            }

            // Step 3: Split text into chunks
            List<string> chunks =
                _textChunkService.SplitText(
                    extractedText,
                    chunkSize: 500,
                    overlap: 100);

            // Step 4: Generate embeddings
            var embeddings = new List<List<float>>();

            try
            {
                foreach (var chunk in chunks)
                {
                    List<float> vector =
                        await _embeddingService
                            .GenerateEmbeddingAsync(chunk);

                    embeddings.Add(vector);
                }
            }
            catch
            {
                // Embedding failed (e.g. Ollama down): don't leave an orphaned upload
                System.IO.File.Delete(filePath);
                throw;
            }

            // Step 5: Store chunks and embeddings
            for (int i = 0; i < chunks.Count; i++)
            {
                _vectorStore.AddDocument(
                    chunks[i],
                    embeddings[i]);
            }

            // Step 6: Return response
            return Ok(new
            {
                message = "PDF processed and embeddings stored successfully",
                fileName = fileName,
                totalChunks = chunks.Count,
                totalEmbeddings = embeddings.Count,
                embeddingDimensions = embeddings.Count > 0
                    ? embeddings[0].Count
                    : 0,
                storedChunks = _vectorStore.GetTotalDocuments()
            });
        }

        // API 3: Search relevant document chunks
        [HttpPost("search")]
        public async Task<IActionResult> Search(
            [FromBody] SearchRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Please enter a question.");
            }

            if (_vectorStore.GetTotalDocuments() == 0)
            {
                return BadRequest(
                    "Please upload and process a PDF first.");
            }

            // Generate question embedding
            List<float> questionVector =
                await _embeddingService.GenerateEmbeddingAsync(
                    request.Question);

            // Retrieve top 3 relevant chunks
            var results = _vectorStore.Search(
                questionVector, topK: 3);

            return Ok(new
            {
                question = request.Question,
                results = results.Select(x => new
                {
                    text = x.Text,
                    similarityScore = x.Score
                })
            });
        }

        // API 4: Generate AI answer using RAG
       

        [HttpPost("ask")]
        public async Task<IActionResult> Ask(
            [FromBody] SearchRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Please enter a question.");
            }

            if (_vectorStore.GetTotalDocuments() == 0)
            {
                return BadRequest(
                    "Please upload and process a PDF first.");
            }

            // Measure total API processing time
            var totalTimer = Stopwatch.StartNew();

            // Step 1: Measure embedding generation
            var embeddingTimer = Stopwatch.StartNew();

            List<float> questionVector =
                await _embeddingService.GenerateEmbeddingAsync(
                    request.Question);

            embeddingTimer.Stop();

            // Step 2: Measure vector similarity search
            var searchTimer = Stopwatch.StartNew();

            var results = _vectorStore.Search(
                questionVector, topK: 3);

            searchTimer.Stop();

            // Step 3: Combine retrieved text chunks
            string context = string.Join(
                "\n\n",
                results.Select(x => x.Text));

            // Step 4: Measure LLM answer generation
            var generationTimer = Stopwatch.StartNew();

            string answer =
                await _chatGenerationService.GenerateAnswerAsync(
                    request.Question,
                    context);

            generationTimer.Stop();
            totalTimer.Stop();

            // Step 5: Return answer and timing details
            return Ok(new
            {
                question = request.Question,
                answer = answer,
                retrievedChunks = results.Count,

                performance = new
                {
                    embeddingMs =
                        embeddingTimer.ElapsedMilliseconds,

                    searchMs =
                        searchTimer.ElapsedMilliseconds,

                    generationMs =
                        generationTimer.ElapsedMilliseconds,

                    totalMs =
                        totalTimer.ElapsedMilliseconds
                }
            });
        }

    }
}
