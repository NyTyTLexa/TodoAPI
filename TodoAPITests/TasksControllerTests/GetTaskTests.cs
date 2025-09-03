using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TodoAPI.Controllers;
using TodoAPI.Data;
using TodoAPI.Models;
using TodoAPI.Service;

namespace TodoAPITests.TasksControllerTests;

public class GetTaskTests
{
    #region Конфигурация
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<RabbitMqService> _rabbitMqServiceMock;

    public GetTaskTests()
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
    #region GetTask_ReturnsFromCache_WhenCacheHit

    /// <summary>
    /// Проверяет, что GetTask возвращает задачу из кэша когда она доступна.
    /// </summary>
    [Fact]
    public async Task GetTask_ReturnsFromCache_WhenCacheHit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var cachedTask = new Tasks { Id = 1, Title = "Cached Task" };
        _cacheServiceMock.Setup(x => x.GetAsync<Tasks>("task_1"))
            .ReturnsAsync(cachedTask);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetTask(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var task = Assert.IsType<Tasks>(okResult.Value);
        Assert.Equal("Cached Task", task.Title);
    }
    #endregion
    #region GetTask_ReturnsFromDbAndCaches_WhenCacheMiss
    /// <summary>
    /// Проверяет, что GetTask возвращает задачу из базы и кэширует её когда кэш пустой.
    /// </summary>
    [Fact]
    public async Task GetTask_ReturnsFromDbAndCaches_WhenCacheMiss()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var task = new Tasks { Id = 1, Title = "DB Task" };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        _cacheServiceMock.Setup(x => x.GetAsync<Tasks>("task_1"))
            .ReturnsAsync((Tasks)null);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetTask(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var resultTask = Assert.IsType<Tasks>(okResult.Value);
        Assert.Equal("DB Task", resultTask.Title);

        _cacheServiceMock.Verify(x => x.SetAsync("task_1", It.IsAny<Tasks>(), TimeSpan.FromMinutes(5)), Times.Once);
    }
    #endregion
    #region GetTask_ReturnsNotFound_WhenTaskNotExists
    /// <summary>
    /// Проверяет, что GetTask возвращает NotFound когда задача не найдена.
    /// </summary>
    [Fact]
    public async Task GetTask_ReturnsNotFound_WhenTaskNotExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _cacheServiceMock.Setup(x => x.GetAsync<Tasks>("task_999"))
            .ReturnsAsync((Tasks)null);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetTask(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion
}
