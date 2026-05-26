using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Finance.PortfolioService.Application.Contracts.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Finance.PortfolioService.Infrastructure.AI;

namespace Finance.PortfolioService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DocumentsController(
    IServiceScopeFactory scopeFactory,
    IOptions<AzureSearchSettings> searchSettings,
    ILogger<DocumentsController> logger,
    IDocumentIngestionService? ingestion = null) : ControllerBase
{
    private readonly AzureSearchSettings _searchSettings = searchSettings.Value;

    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    [HttpPost("ingest")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult IngestDocument([FromForm] IFormFile file)
    {
        if (ingestion is null)
            return StatusCode(503, "Document ingestion service is not configured.");

        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files are accepted.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File exceeds the 20 MB limit.");

        var fileName = Path.GetFileName(file.FileName);

        // Copy to MemoryStream so the IFormFile lifetime doesn't matter in the background task
        var ms = new MemoryStream();
        file.CopyTo(ms);
        ms.Position = 0;

        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<IDocumentIngestionService>();
            try
            {
                await svc.IngestAsync(ms, fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background ingestion failed for '{FileName}'.", fileName);
            }
            finally
            {
                await ms.DisposeAsync();
            }
        });

        return Accepted(new { message = $"Ingestion started for '{fileName}'." });
    }

    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IEnumerable<string>>> ListDocuments(CancellationToken cancellationToken)
    {
        if (!_searchSettings.IsConfigured)
            return StatusCode(503, "Azure AI Search is not configured.");

        var searchClient = new SearchClient(
            new Uri(_searchSettings.Endpoint),
            _searchSettings.IndexName,
            new AzureKeyCredential(_searchSettings.AdminKey));

        var options = new SearchOptions
        {
            Facets = { "sourceFile,count:1000" },
            Size = 0
        };

        var response = await searchClient.SearchAsync<SearchDocument>("*", options, cancellationToken);
        var facets = response.Value.Facets;

        if (facets is null || !facets.TryGetValue("sourceFile", out var sourceFileFacets))
            return Ok(Array.Empty<string>());

        var names = sourceFileFacets.Select(f => f.Value?.ToString() ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n)
            .ToList();

        return Ok(names);
    }
}
