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
    private readonly IYahooFinanceService _yahooFinanceService;

    public SentimentController(
        ISentimentAnalysisService sentimentService,
        IYahooFinanceService yahooFinanceService)
    {
        _sentimentService = sentimentService;
        _yahooFinanceService = yahooFinanceService;
    }

    [HttpGet("analyze-yahoo-news/{ticker}")]
    public async Task<IActionResult> AnalyzeYahooFinanceNews(string ticker)
    {
        var articles = await _yahooFinanceService.FetchLatestStockNews(ticker);

        var results = articles.Select(article => new
        {
            Article = article,
            Sentiment = _sentimentService.PredictSentiment(article)
        });

        return Ok(results);
    }
}
