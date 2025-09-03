using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TodoAPI.Controllers;
using TodoAPI.Data;
using TodoAPI.Models;
using TodoAPI.Service;

namespace TodoAPITests.TasksControllerTests;

public class DeleteTaskTests
{
    #region Конфигурация
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<RabbitMqService> _rabbitMqServiceMock;

    public DeleteTaskTests()
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
    #region DeleteTask_DeletesTaskAndClearsCache_WhenTaskExists

    /// <summary>
    /// Проверяет, что DeleteTask удаляет задачу и очищает кэш.
    /// </summary>
    [Fact]
    public async Task DeleteTask_DeletesTaskAndClearsCache_WhenTaskExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var task = new Tasks { Id = 1, Title = "To Delete" };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.DeleteTask(1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedTask = await context.Tasks.FindAsync(1);
        Assert.Null(deletedTask);

        _cacheMock.Verify(x => x.RemoveAsync("all_tasks", default), Times.Once);
    }
    #endregion
    #region DeleteTask_ReturnsNotFound_WhenTaskNotExists
    /// <summary>
    /// Проверяет, что DeleteTask возвращает NotFound когда задача не существует.
    /// </summary>
    [Fact]
    public async Task DeleteTask_ReturnsNotFound_WhenTaskNotExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.DeleteTask(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
