using MediatR;
using SmartTask.Application.Common.Interfaces;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Common;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Exceptions;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Application.Features.Tasks.Queries;

// ── GET ALL ──────────────────────────────────────────────────────────────────

public record GetTasksQuery(string? StatusFilter = null) : IRequest<Result<IReadOnlyList<TaskDto>>>;

public class GetTasksHandler : IRequestHandler<GetTasksQuery, Result<IReadOnlyList<TaskDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetTasksHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        => (_uow, _currentUser) = (uow, currentUser);

    public async Task<Result<IReadOnlyList<TaskDto>>> Handle(GetTasksQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedDomainException();

        var tasks = await _uow.Repository<TaskItem>().FindAsync(
            t => t.UserId == userId && !t.IsDeleted, ct);

        var filtered = request.StatusFilter?.ToLower() switch
        {
            "overdue" => tasks.Where(t => t.IsOverdue()),
            "today"   => tasks.Where(t => t.DueDate?.Date == DateTime.UtcNow.Date),
            "pending" => tasks.Where(t => t.Status == TaskStatus.Pending),
            _         => tasks
        };

        return Result<IReadOnlyList<TaskDto>>.Success(
            filtered.Select(t => t.ToDto()).ToList());
    }
}

// ── SUMMARY ──────────────────────────────────────────────────────────────────

public record GetTaskSummaryQuery : IRequest<Result<TaskSummaryDto>>;

public class GetTaskSummaryHandler : IRequestHandler<GetTaskSummaryQuery, Result<TaskSummaryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetTaskSummaryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        => (_uow, _currentUser) = (uow, currentUser);

    public async Task<Result<TaskSummaryDto>> Handle(GetTaskSummaryQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedDomainException();
        var tasks = await _uow.Repository<TaskItem>().FindAsync(
            t => t.UserId == userId && !t.IsDeleted, ct);

        var today = tasks
            .Where(t => t.DueDate?.Date == DateTime.UtcNow.Date && t.Status != TaskStatus.Completed)
            .Select(t => t.ToDto())
            .ToList();

        return Result<TaskSummaryDto>.Success(new TaskSummaryDto(
            Total:       tasks.Count,
            Pending:     tasks.Count(t => t.Status == TaskStatus.Pending),
            InProgress:  tasks.Count(t => t.Status == TaskStatus.InProgress),
            Completed:   tasks.Count(t => t.Status == TaskStatus.Completed),
            Overdue:     tasks.Count(t => t.IsOverdue()),
            UpcomingToday: today
        ));
    }
}
