using LevelUp.Application.Recommendations;
using LevelUp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LevelUp.Api.Controllers;

/// <summary>Stored recommendations, the "Help Me!" suggestion endpoint and recommendation history.</summary>
[ApiController]
[Route("api/recommendations")]
[Produces("application/json")]
public sealed class RecommendationsController(IRecommendationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RecommendationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecommendationDto>>> List(
        [FromQuery] RecommendationCategory? category, [FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await service.ListAsync(category, includeInactive, ct));

    [HttpGet("{id:int}", Name = nameof(GetRecommendation))]
    [ProducesResponseType<RecommendationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationDto>> GetRecommendation(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<RecommendationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecommendationDto>> Create(SaveRecommendationRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetRecommendation), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<RecommendationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationDto>> Update(int id, SaveRecommendationRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>"Help Me!" - returns a suggestion for the given context and records it in history.</summary>
    [HttpPost("suggest")]
    [ProducesResponseType<ShownRecommendationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShownRecommendationDto>> Suggest(RecommendationRequest request, CancellationToken ct) =>
        Ok(await service.GetSuggestionAsync(request, ct));

    /// <summary>Recommendation history, optionally filtered by activity or goal.</summary>
    [HttpGet("history")]
    [ProducesResponseType<IReadOnlyList<RecommendationHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecommendationHistoryDto>>> History(
        [FromQuery] int? activityId, [FromQuery] int? goalId, [FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await service.GetHistoryAsync(activityId, goalId, take, ct));
}
