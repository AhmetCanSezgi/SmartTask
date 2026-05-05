using FluentAssertions;
using Xunit;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Domain.Exceptions;
using Priority = SmartTask.Domain.Enums.Priority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Tests.Domain;

/// <summary>
/// TaskItem domain entity'sinin tüm kurallarını test eder.
/// Dış bağımlılık yok — saf domain testi.
/// </summary>
public class TaskItemTests
{
    private static readonly Guid _userId = Guid.NewGuid();

    // ── CREATE ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Act
        var task = TaskItem.Create("Test görevi", _userId, priority: Priority.High);

        // Assert
        task.Title.Should().Be("Test görevi");
        task.Priority.Should().Be(Priority.High);
        task.Status.Should().Be(TaskStatus.Pending);
        task.UserId.Should().Be(_userId);
        task.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ShouldThrowDomainException(string? title)
    {
        // Act
        var act = () => TaskItem.Create(title!, _userId);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*title*");
    }

    [Fact]
    public void Create_TitleShouldBeTrimmed()
    {
        var task = TaskItem.Create("  Başlık  ", _userId);
        task.Title.Should().Be("Başlık");
    }

    // ── COMPLETE ────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_PendingTask_ShouldSetStatusCompleted()
    {
        var task = TaskItem.Create("Görev", _userId);

        task.Complete();

        task.Status.Should().Be(TaskStatus.Completed);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_AlreadyCompletedTask_ShouldThrowDomainException()
    {
        var task = TaskItem.Create("Görev", _userId);
        task.Complete();

        var act = () => task.Complete();

        act.Should().Throw<DomainException>()
           .WithMessage("*already completed*");
    }

    [Fact]
    public void Complete_CancelledTask_ShouldThrowDomainException()
    {
        var task = TaskItem.Create("Görev", _userId);
        task.Cancel();

        var act = () => task.Complete();

        act.Should().Throw<DomainException>()
           .WithMessage("*Cancelled*");
    }

    // ── START ───────────────────────────────────────────────────────────────

    [Fact]
    public void Start_PendingTask_ShouldSetStatusInProgress()
    {
        var task = TaskItem.Create("Görev", _userId);

        task.Start();

        task.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public void Start_CompletedTask_ShouldThrowDomainException()
    {
        var task = TaskItem.Create("Görev", _userId);
        task.Complete();

        var act = () => task.Start();

        act.Should().Throw<DomainException>();
    }

    // ── CANCEL ──────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_PendingTask_ShouldSetStatusCancelled()
    {
        var task = TaskItem.Create("Görev", _userId);

        task.Cancel();

        task.Status.Should().Be(TaskStatus.Cancelled);
    }

    [Fact]
    public void Cancel_CompletedTask_ShouldThrowDomainException()
    {
        var task = TaskItem.Create("Görev", _userId);
        task.Complete();

        var act = () => task.Cancel();

        act.Should().Throw<DomainException>();
    }

    // ── OVERDUE ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsOverdue_PastDueDateAndPending_ShouldReturnTrue()
    {
        var task = TaskItem.Create("Görev", _userId,
            dueDate: DateTime.UtcNow.AddDays(-1));

        task.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_FutureDueDate_ShouldReturnFalse()
    {
        var task = TaskItem.Create("Görev", _userId,
            dueDate: DateTime.UtcNow.AddDays(1));

        task.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_CompletedTaskWithPastDueDate_ShouldReturnFalse()
    {
        var task = TaskItem.Create("Görev", _userId,
            dueDate: DateTime.UtcNow.AddDays(-1));
        task.Complete();

        // Tamamlanmış task gecikmiş sayılmaz
        task.IsOverdue().Should().BeFalse();
    }

    // ── SOFT DELETE ─────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedTrue()
    {
        var task = TaskItem.Create("Görev", _userId);

        task.SoftDelete();

        task.IsDeleted.Should().BeTrue();
        task.UpdatedAt.Should().NotBeNull();
    }
}
