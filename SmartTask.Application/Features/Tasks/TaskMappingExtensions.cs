using SmartTask.Application.DTOs;
using SmartTask.Domain.Entities;

namespace SmartTask.Application.Features.Tasks;

public static class TaskMappingExtensions
{
    public static TaskDto ToDto(this TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Priority.ToString(),
        task.Status.ToString(),
        task.DueDate,
        task.IsOverdue(),
        task.CreatedAt
    );
}
