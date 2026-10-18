
namespace QMS.Chatbot.Api.Services
{
    public class VectorStoreService
    {
        private readonly List<(string Text, List<float> Vector)>
            _documents = new();

        private readonly object _lock = new();

        public void AddDocument(string text, List<float> vector)
        {
            lock (_lock)
            {
                _documents.Add((text, vector.ToList()));
            }
        }

        public int GetTotalDocuments()
        {
            lock (_lock)
            {
                return _documents.Count;
            }
        }

        // Find the most relevant chunks
        public List<(string Text, double Score)> Search(
            List<float> questionVector,
            int topK = 3)
        {
            List<(string Text, List<float> Vector)> snapshot;

            lock (_lock)
            {
                snapshot = _documents.ToList();
            }

            return snapshot
                .Select(doc => (
                    Text: doc.Text,
                    Score: CosineSimilarity(
                        questionVector, doc.Vector)
                ))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();
        }

        // Calculate cosine similarity
        private static double CosineSimilarity(
            List<float> vectorA,
            List<float> vectorB)
        {
            if (vectorA.Count != vectorB.Count)
                throw new ArgumentException(
                    "Vector dimensions do not match.");

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < vectorA.Count; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct /
                (Math.Sqrt(magnitudeA) *
                 Math.Sqrt(magnitudeB));
        }
    }
}
