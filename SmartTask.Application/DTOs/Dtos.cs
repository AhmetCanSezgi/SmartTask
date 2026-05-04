using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    string Status,
    DateTime? DueDate,
    bool IsOverdue,
    DateTime CreatedAt
);

public record TaskSummaryDto(
    int Total,
    int Pending,
    int InProgress,
    int Completed,
    int Overdue,
    List<TaskDto> UpcomingToday
);

public record AuthDto(
    string AccessToken,
    string RefreshToken,
    string Username
);
