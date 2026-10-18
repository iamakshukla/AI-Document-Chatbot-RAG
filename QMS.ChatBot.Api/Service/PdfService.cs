
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace QMS.Chatbot.Api.Services
{
    public class PdfService
    {
        public string ExtractText(string filePath)
        {
            var text = new StringBuilder();

            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    text.AppendLine(
                        ContentOrderTextExtractor.GetText(page)
                    );
                }
            }

            return text.ToString();
        }
    }
}
