using LevelUp.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace LevelUp.Api.Controllers;

/// <summary>Aggregated scores, goal statistics and trend data for the dashboard.</summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>Today's / weekly / monthly score plus goal counts.</summary>
    /// <param name="date">Reference day (defaults to the server's today).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] DateOnly? date, CancellationToken ct) =>
        Ok(await service.GetSummaryAsync(date, ct));

    /// <summary>
    /// Trend lines for activities (sum of scores per bucket) and goals (progress % at bucket end).
    /// Daily = last 30 days, Weekly = last 12 weeks, Monthly = last 12 months, Annual = last 5 years.
    /// </summary>
    [HttpGet("trends")]
    [ProducesResponseType<TrendResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrendResponseDto>> GetTrends(
        [FromQuery] TrendPeriod period = TrendPeriod.Daily, [FromQuery] DateOnly? endDate = null, CancellationToken ct = default) =>
        Ok(await service.GetTrendsAsync(period, endDate, ct));
}
