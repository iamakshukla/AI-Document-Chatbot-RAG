
namespace QMS.Chatbot.Api.Services
{
    public class TextChunkService
    {
        public List<string> SplitText(
            string text,
            int chunkSize = 500,
            int overlap = 100)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            if (chunkSize <= 0 ||
                overlap < 0 ||
                overlap >= chunkSize)
            {
                throw new ArgumentException(
                    "Invalid chunk size or overlap.");
            }

            int start = 0;

            while (start < text.Length)
            {
                int length = Math.Min(
                    chunkSize,
                    text.Length - start);

                string chunk = text.Substring(start, length);

                chunks.Add(chunk);

                if (start + length >= text.Length)
                    break;

                start += chunkSize - overlap;
            }

            return chunks;
        }
    }
}
