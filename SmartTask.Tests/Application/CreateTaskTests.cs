using FluentAssertions;
using Xunit;
using Moq;
using SmartTask.Application.Common.Interfaces;
using SmartTask.Application.Features.Tasks.Commands;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Exceptions;
using Priority = SmartTask.Domain.Enums.Priority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Tests.Application;

/// <summary>
/// Application handler testleri.
/// IUnitOfWork ve ICurrentUserService mock'lanır —
/// gerçek veritabanı bağlantısı gerekmez.
/// </summary>
public class CreateTaskTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IRepository<TaskItem>> _taskRepoMock = new();

    private readonly Guid _userId = Guid.NewGuid();

    public CreateTaskTests()
    {
        // Her testte ortak setup
        _currentUserMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _uowMock.Setup(x => x.Repository<TaskItem>()).Returns(_taskRepoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateTaskAndReturnDto()
    {
        // Arrange
        var handler = new CreateTaskHandler(_uowMock.Object, _currentUserMock.Object);
        var command = new CreateTaskCommand("Test görevi", null, "High", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Test görevi");
        result.Value.Priority.Should().Be("High");
        result.Value.Status.Should().Be("Pending");

        // Repository'e AddAsync çağrıldı mı?
        _taskRepoMock.Verify(
            x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // SaveChanges çağrıldı mı?
        _uowMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new CreateTaskHandler(_uowMock.Object, _currentUserMock.Object);
        var command = new CreateTaskCommand("Test", null, "Medium", null);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedDomainException>();
    }

    [Fact]
    public async Task Handle_WithDueDate_ShouldSetDueDateCorrectly()
    {
        // Arrange
        var dueDate = DateTime.UtcNow.AddDays(3);
        var handler = new CreateTaskHandler(_uowMock.Object, _currentUserMock.Object);
        var command = new CreateTaskCommand("Görev", "Açıklama", "Low", dueDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DueDate.Should().BeCloseTo(dueDate, TimeSpan.FromSeconds(1));
    }
}

public class CompleteTaskTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IRepository<TaskItem>> _taskRepoMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public CompleteTaskTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(_userId);
        _uowMock.Setup(x => x.Repository<TaskItem>()).Returns(_taskRepoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_ValidTask_ShouldCompleteSuccessfully()
    {
        // Arrange
        var task = TaskItem.Create("Görev", _userId);
        _taskRepoMock.Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(task);

        var handler = new CompleteTaskHandler(_uowMock.Object, _currentUserMock.Object);

        // Act
        var result = await handler.Handle(new CompleteTaskCommand(task.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(TaskStatus.Completed);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _taskRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((TaskItem?)null);

        var handler = new CompleteTaskHandler(_uowMock.Object, _currentUserMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new CompleteTaskCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OtherUsersTask_ShouldThrowUnauthorizedException()
    {
        // Arrange — task başka kullanıcıya ait
        var otherUserId = Guid.NewGuid();
        var task = TaskItem.Create("Görev", otherUserId);

        _taskRepoMock.Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(task);

        var handler = new CompleteTaskHandler(_uowMock.Object, _currentUserMock.Object);

        // Act
        var act = async () => await handler.Handle(
            new CompleteTaskCommand(task.Id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedDomainException>();
    }
}
