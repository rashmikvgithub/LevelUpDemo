using LevelUp.Application.Goals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LevelUp.Api.Controllers;

/// <summary>Long-term goals and their progress checkpoints.</summary>
[ApiController]
[Route("api/goals")]
[Produces("application/json")]
public sealed class GoalsController(IGoalService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GoalDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GoalDto>>> Search([FromQuery] GoalQuery query, CancellationToken ct) =>
        Ok(await service.SearchAsync(query, ct));

    /// <summary>Gets a goal including its full checkpoint history.</summary>
    [HttpGet("{id:int}", Name = nameof(GetGoal))]
    [ProducesResponseType<GoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoalDto>> GetGoal(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<GoalDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoalDto>> Create(CreateGoalRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetGoal), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<GoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoalDto>> Update(int id, UpdateGoalRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Marks the goal as completed by logging a 100% checkpoint.</summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType<GoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GoalDto>> Complete(
        int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CompleteGoalRequest? request, CancellationToken ct) =>
        Ok(await service.CompleteAsync(id, request ?? new CompleteGoalRequest(null), ct));

    [HttpGet("{id:int}/checkpoints")]
    [ProducesResponseType<IReadOnlyList<GoalCheckpointDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GoalCheckpointDto>>> GetCheckpoints(int id, CancellationToken ct) =>
        Ok(await service.GetCheckpointsAsync(id, ct));

    /// <summary>Logs a progress checkpoint (percent complete + note) for a goal.</summary>
    [HttpPost("{id:int}/checkpoints")]
    [ProducesResponseType<GoalCheckpointDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoalCheckpointDto>> AddCheckpoint(int id, CreateGoalCheckpointRequest request, CancellationToken ct)
    {
        var created = await service.AddCheckpointAsync(id, request, ct);
        return CreatedAtAction(nameof(GetCheckpoints), new { id }, created);
    }

    [HttpDelete("{id:int}/checkpoints/{checkpointId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCheckpoint(int id, int checkpointId, CancellationToken ct)
    {
        await service.DeleteCheckpointAsync(id, checkpointId, ct);
        return NoContent();
    }
}
