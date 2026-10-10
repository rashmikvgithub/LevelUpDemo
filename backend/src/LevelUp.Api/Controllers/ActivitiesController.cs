using LevelUp.Application.Activities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LevelUp.Api.Controllers;

/// <summary>Daily activities and their self-evaluation scores.</summary>
[ApiController]
[Route("api/activities")]
[Produces("application/json")]
public sealed class ActivitiesController(IActivityService service) : ControllerBase
{
    /// <summary>Searches activities by text, status, date added and date retired.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ActivityDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> Search([FromQuery] ActivityQuery query, CancellationToken ct) =>
        Ok(await service.SearchAsync(query, ct));

    /// <summary>Gets all activities tracked on a day together with that day's grade (if any).</summary>
    [HttpGet("daily")]
    [ProducesResponseType<IReadOnlyList<DailyActivityDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyActivityDto>>> GetDaily([FromQuery, BindRequired] DateOnly date, CancellationToken ct) =>
        Ok(await service.GetDailyAsync(date, ct));

    [HttpGet("{id:int}", Name = nameof(GetActivity))]
    [ProducesResponseType<ActivityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityDto>> GetActivity(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<ActivityDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivityDto>> Create(CreateActivityRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetActivity), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ActivityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityDto>> Update(int id, UpdateActivityRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Deletes an activity and all of its scores.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Lists the grades recorded for an activity (newest first).</summary>
    [HttpGet("{id:int}/scores")]
    [ProducesResponseType<IReadOnlyList<ActivityScoreDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActivityScoreDto>>> GetScores(
        int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        Ok(await service.GetScoresAsync(id, from, to, ct));

    /// <summary>Creates or updates the grade (0-5) for an activity on a given date.</summary>
    [HttpPut("{id:int}/scores")]
    [ProducesResponseType<SaveActivityScoreResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaveActivityScoreResult>> SaveScore(int id, SaveActivityScoreRequest request, CancellationToken ct) =>
        Ok(await service.SaveScoreAsync(id, request, ct));

    [HttpDelete("{id:int}/scores/{scoreId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScore(int id, int scoreId, CancellationToken ct)
    {
        await service.DeleteScoreAsync(id, scoreId, ct);
        return NoContent();
    }
}
