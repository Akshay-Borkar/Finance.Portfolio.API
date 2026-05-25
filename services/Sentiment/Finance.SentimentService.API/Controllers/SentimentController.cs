using Finance.SentimentService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.SentimentService.API.Controllers;

[Route("api/sentiment")]
[ApiController]
[Authorize]
public class SentimentController : ControllerBase
{
    private readonly ISentimentAnalysisService _sentimentService;
    private readonly IMarketAuxService _marketAuxService;

    public SentimentController(
        ISentimentAnalysisService sentimentService,
        IMarketAuxService marketAuxService)
    {
        _sentimentService = sentimentService;
        _marketAuxService = marketAuxService;
    }

    [HttpGet("analyze/{ticker}")]
    public async Task<IActionResult> AnalyzeStockNews(string ticker)
    {
        var articles = await _marketAuxService.FetchLatestStockNews(ticker);

        var results = articles.Select(article => new
        {
            Article = article,
            Sentiment = _sentimentService.PredictSentiment(article)
        });

        return Ok(results);
    }
}
