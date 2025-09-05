using Xunit;
using Moq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using TodoAPI.Controllers;
using TodoAPI.Models;
using TodoAPI.Service;
using TodoAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Caching.Distributed;
using TodoAPI.Interface;
namespace TodoAPITests.TasksControllerTests;
public class GetAllTasksTests
{
    #region Конфигурация
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<RabbitMqService> _rabbitMqServiceMock;

    public GetAllTasksTests()
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
    #region GetAllTasks_ReturnsFromCache_WhenCacheHit

    /// <summary>
    /// Проверяет, что GetAllTasks возвращает данные из кэша когда они доступны.
    /// </summary>
    [Fact]
    public async Task GetAllTasks_ReturnsFromCache_WhenCacheHit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var cachedTasks = new List<Tasks> { new Tasks { Id = 1, Title = "Test Task" } };
        _cacheServiceMock.Setup(x => x.GetAsync<List<Tasks>>("all_tasks"))
            .ReturnsAsync(cachedTasks);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetAllTasks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tasks = Assert.IsType<List<Tasks>>(okResult.Value);
        Assert.Single(tasks);
        Assert.Equal("Test Task", tasks[0].Title);
    }
    #endregion
    #region GetAllTasks_ReturnsFromDbAndCaches_WhenCacheMiss
    /// <summary>
    /// Проверяет, что GetAllTasks возвращает данные из базы и кэширует их когда кэш пустой.
    /// </summary>
    [Fact]
    public async Task GetAllTasks_ReturnsFromDbAndCaches_WhenCacheMiss()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var task = new Tasks { Id = 1, Title = "Test Task", CreatedAt = DateTime.UtcNow };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        _cacheServiceMock.Setup(x => x.GetAsync<List<Tasks>>("all_tasks"))
            .ReturnsAsync((List<Tasks>)null);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetAllTasks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tasks = Assert.IsType<List<Tasks>>(okResult.Value);
        Assert.Single(tasks);
        Assert.Equal("Test Task", tasks[0].Title);

        _cacheServiceMock.Verify(x => x.SetAsync("all_tasks", It.IsAny<List<Tasks>>(), TimeSpan.FromMinutes(5)), Times.Once);
    }

    #endregion
}
