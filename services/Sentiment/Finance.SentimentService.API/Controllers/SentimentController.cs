using Finance.Integrations.MarketAux;
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
    private readonly IMarketAuxNewsClient _marketAuxNews;

    public SentimentController(
        ISentimentAnalysisService sentimentService,
        IMarketAuxNewsClient marketAuxNews)
    {
        _sentimentService = sentimentService;
        _marketAuxNews = marketAuxNews;
    }

    [HttpGet("analyze/{ticker}")]
    public async Task<IActionResult> AnalyzeStockNews(string ticker, CancellationToken cancellationToken)
    {
        var articles = await _marketAuxNews.GetHeadlinesAsync(ticker, cancellationToken);

        var results = articles.Select(article => new
        {
            Article = article,
            Sentiment = _sentimentService.PredictSentiment(article)
        });

        return Ok(results);
    }
}
