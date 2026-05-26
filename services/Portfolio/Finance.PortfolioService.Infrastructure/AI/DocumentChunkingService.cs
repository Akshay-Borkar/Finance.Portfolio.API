using UglyToad.PdfPig;

namespace Finance.PortfolioService.Infrastructure.AI;

public class DocumentChunkingService
{
    private const int TargetWords = 500;
    private const int OverlapWords = 50;

    public List<DocumentChunk> ChunkPdf(Stream pdfStream, string fileName)
    {
        var chunks = new List<DocumentChunk>();

        using var document = PdfDocument.Open(pdfStream);

        // Collect (text, pageNumber) pairs per page
        var pages = document.GetPages()
            .Select(p => (Text: p.Text, PageNumber: p.Number))
            .ToList();

        // Flatten all words with their originating page number
        var words = new List<(string Word, int Page)>();
        foreach (var (text, pageNum) in pages)
        {
            foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                words.Add((word, pageNum));
        }

        int chunkIndex = 0;
        int start = 0;

        while (start < words.Count)
        {
            int end = Math.Min(start + TargetWords, words.Count);
            var slice = words[start..end];

            var content = string.Join(' ', slice.Select(w => w.Word));
            var dominantPage = slice
                .GroupBy(w => w.Page)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var safeKey = System.Text.RegularExpressions.Regex.Replace(fileName, @"[^a-zA-Z0-9_\-=]", "_");
            var id = $"{safeKey}_{chunkIndex}";
            chunks.Add(new DocumentChunk(id, content, fileName, dominantPage, chunkIndex));

            chunkIndex++;
            start += TargetWords - OverlapWords;
        }

        return chunks;
    }
}
