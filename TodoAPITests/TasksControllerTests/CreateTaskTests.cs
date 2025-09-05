using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TodoAPI.Controllers;
using TodoAPI.Data;
using TodoAPI.Interface;
using TodoAPI.Models;
using TodoAPI.Service;

namespace TodoAPITests.TasksControllerTests;

public class CreateTaskTests
{
    #region Конфигурация
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<RabbitMqService> _rabbitMqServiceMock;

    public CreateTaskTests()
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
    #region CreateTask_CreatesTaskAndClearsCache_WhenValidTask

    /// <summary>
    /// Проверяет, что CreateTask создает новую задачу и очищает кэш.
    /// </summary>
    [Fact]
    public async Task CreateTask_CreatesTaskAndClearsCache_WhenValidTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var newTask = new Tasks { Title = "New Task", Description = "Description" };

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.CreateTask(newTask);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdTask = Assert.IsType<Tasks>(createdResult.Value);
        Assert.Equal("New Task", createdTask.Title);
        Assert.True(createdTask.Id > 0);
        Assert.True(createdTask.CreatedAt > DateTime.MinValue);

        _cacheMock.Verify(x => x.RemoveAsync("all_tasks", default), Times.Once);
    }

    #endregion
}