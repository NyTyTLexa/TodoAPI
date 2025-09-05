using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TodoAPI.Controllers;
using TodoAPI.Data;
using TodoAPI.Enum;
using TodoAPI.Interface;
using TodoAPI.Models;
using TodoAPI.Service;

namespace TodoAPITests.TasksControllerTests;

public class UpdateTaskTests
{
    #region Конфигурация
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<RabbitMqService> _rabbitMqServiceMock;

    public UpdateTaskTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _cacheServiceMock = new Mock<ICacheService>();
        _rabbitMqServiceMock = new Mock<RabbitMqService>();
    }

    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
    #endregion
    #region UpdateTask_UpdatesTaskAndClearsCache_WhenValidTask

    /// <summary>
    /// Проверяет, что UpdateTask обновляет задачу и очищает кэш.
    /// </summary>
    [Fact]
    public async Task UpdateTask_UpdatesTaskAndClearsCache_WhenValidTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var existingTask = new Tasks { Id = 1, Title = "Original", Status =TasksStatus.active };
        context.Tasks.Add(existingTask);
        await context.SaveChangesAsync();

        var updatedTask = new Tasks { Id = 1, Title = "Updated", Status = TasksStatus.active };
        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.UpdateTask(1, updatedTask);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var taskFromDb = await context.Tasks.FindAsync(1);
        Assert.Equal("Updated", taskFromDb.Title);

        _cacheMock.Verify(x => x.RemoveAsync("all_tasks", default), Times.Once);
    }
    #endregion
    #region UpdateTask_PublishesStatusChange_WhenStatusChanged
    /// <summary>
    /// Проверяет, что UpdateTask публикует сообщение в RabbitMQ при изменении статуса.
    /// </summary>
    [Fact]
    public async Task UpdateTask_PublishesStatusChange_WhenStatusChanged()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var existingTask = new Tasks { Id = 1, Title = "Task", Status = TasksStatus.active };
        context.Tasks.Add(existingTask);
        await context.SaveChangesAsync();

        var updatedTask = new Tasks { Id = 1, Title = "Task", Status = TasksStatus.completed };
        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.UpdateTask(1, updatedTask);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _rabbitMqServiceMock.Verify(x => x.Publish("task.status.changed",
            It.Is<object>(o => o.GetType().GetProperty("TaskId").GetValue(o).Equals(1) &&
                              o.GetType().GetProperty("NewStatus").GetValue(o).Equals("completed"))),
            Times.Once);
    }
    #endregion
    #region UpdateTask_ReturnsBadRequest_WhenIdMismatch
    /// <summary>
    /// Проверяет, что UpdateTask возвращает BadRequest когда ID не совпадают.
    /// </summary>
    [Fact]
    public async Task UpdateTask_ReturnsBadRequest_WhenIdMismatch()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var updatedTask = new Tasks { Id = 2, Title = "Task" };
        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.UpdateTask(1, updatedTask);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }
    #endregion
    #region UpdateTask_ReturnsNotFound_WhenTaskNotExists
    /// <summary>
    /// Проверяет, что UpdateTask возвращает NotFound когда задача не существует.
    /// </summary>
    [Fact]
    public async Task UpdateTask_ReturnsNotFound_WhenTaskNotExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var updatedTask = new Tasks { Id = 999, Title = "Task" };
        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.UpdateTask(999, updatedTask);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

}
