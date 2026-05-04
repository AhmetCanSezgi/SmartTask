using SmartTask.Domain.Common;
using SmartTask.Domain.Exceptions;
using Priority = SmartTask.Domain.Enums.Priority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;
namespace SmartTask.Domain.Entities;

/// <summary>
/// Core domain entity. Setter'lar private — state sadece
/// domain metotları üzerinden değiştirilebilir.
/// </summary>
public sealed class TaskItem : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public Priority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid UserId { get; private set; }

    // EF Core için parametresiz constructor
    private TaskItem() { }

    public static TaskItem Create(
        string title,
        Guid userId,
        string? description = null,
        Priority priority = Priority.Medium,
        DateTime? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Task title cannot be empty.");

        return new TaskItem
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            Priority = priority,
            Status = TaskStatus.Pending,
            DueDate = dueDate,
            UserId = userId
        };
    }

    public void Update(string title, string? description, Priority priority, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Task title cannot be empty.");

        if (Status == TaskStatus.Completed)
            throw new DomainException("Completed tasks cannot be updated.");

        Title = title.Trim();
        Description = description?.Trim();
        Priority = priority;
        DueDate = dueDate;
        SetUpdated();
    }

    public void Start()
    {
        if (Status != TaskStatus.Pending)
            throw new DomainException($"Only pending tasks can be started. Current: {Status}");
        Status = TaskStatus.InProgress;
        SetUpdated();
    }

    public void Complete()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("Task is already completed.");
        if (Status == TaskStatus.Cancelled)
            throw new DomainException("Cancelled tasks cannot be completed.");
        Status = TaskStatus.Completed;
        SetUpdated();
    }

    public void Cancel()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("Completed tasks cannot be cancelled.");
        Status = TaskStatus.Cancelled;
        SetUpdated();
    }

    public bool IsOverdue() =>
        DueDate.HasValue &&
        DueDate.Value < DateTime.UtcNow &&
        Status is TaskStatus.Pending or TaskStatus.InProgress;
}
