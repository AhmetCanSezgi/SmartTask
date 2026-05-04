using FluentValidation;
using MediatR;
using SmartTask.Application.Common.Interfaces;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Common;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Domain.Exceptions;

namespace SmartTask.Application.Features.Tasks.Commands;

// ── CREATE ──────────────────────────────────────────────────────────────────

public record CreateTaskCommand(
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDate
) : IRequest<Result<TaskDto>>;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Priority).IsEnumName(typeof(Priority), caseSensitive: false);
        RuleFor(x => x.DueDate).GreaterThan(DateTime.UtcNow).When(x => x.DueDate.HasValue);
    }
}

public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateTaskHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        => (_uow, _currentUser) = (uow, currentUser);

    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedDomainException();

        var priority = Enum.Parse<Priority>(request.Priority, ignoreCase: true);
        var task = TaskItem.Create(request.Title, userId, request.Description, priority, request.DueDate);

        await _uow.Repository<TaskItem>().AddAsync(task, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<TaskDto>.Success(task.ToDto());
    }
}

// ── COMPLETE ────────────────────────────────────────────────────────────────

public record CompleteTaskCommand(Guid TaskId) : IRequest<Result>;

public class CompleteTaskHandler : IRequestHandler<CompleteTaskCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CompleteTaskHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        => (_uow, _currentUser) = (uow, currentUser);

    public async Task<Result> Handle(CompleteTaskCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedDomainException();
        var task = await _uow.Repository<TaskItem>().GetByIdAsync(request.TaskId, ct)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        if (task.UserId != userId) throw new UnauthorizedDomainException();

        task.Complete();
        _uow.Repository<TaskItem>().Update(task);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// ── DELETE (Soft) ────────────────────────────────────────────────────────────

public record DeleteTaskCommand(Guid TaskId) : IRequest<Result>;

public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public DeleteTaskHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        => (_uow, _currentUser) = (uow, currentUser);

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedDomainException();
        var task = await _uow.Repository<TaskItem>().GetByIdAsync(request.TaskId, ct)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        if (task.UserId != userId) throw new UnauthorizedDomainException();

        task.SoftDelete();
        _uow.Repository<TaskItem>().Update(task);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
