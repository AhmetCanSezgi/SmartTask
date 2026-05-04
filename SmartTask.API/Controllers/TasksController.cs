using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTask.Application.Features.Tasks.Commands;
using SmartTask.Application.Features.Tasks.Queries;

namespace SmartTask.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator) => _mediator = mediator;

    /// <summary>Kullanıcının tüm task'larını listeler. filter: all|pending|today|overdue</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? filter, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTasksQuery(filter), ct);
        return result.Match<IActionResult>(
            tasks => Ok(tasks),
            error => BadRequest(new { error }));
    }

    /// <summary>Özet istatistik döner</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTaskSummaryQuery(), ct);
        return result.Match<IActionResult>(
            summary => Ok(summary),
            error => BadRequest(new { error }));
    }

    /// <summary>Yeni task oluşturur</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.Match<IActionResult>(
            task => CreatedAtAction(nameof(GetAll), new { id = task.Id }, task),
            error => BadRequest(new { error }));
    }

    /// <summary>Task'ı tamamlandı olarak işaretler</summary>
    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteTaskCommand(id), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { result.Error });
    }

    /// <summary>Task'ı soft-delete ile siler</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteTaskCommand(id), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { result.Error });
    }
}
