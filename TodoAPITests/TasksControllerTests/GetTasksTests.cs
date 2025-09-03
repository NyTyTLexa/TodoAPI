using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using TodoAPI.Controllers;
using TodoAPI.Service;
using TodoAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using TodoAPI.Models;
using TodoAPITests.Models;
using Microsoft.Extensions.Caching.Distributed;
namespace TodoAPITests.TasksControllerTests;
public class GetTasksTests
{
    #region Конфигурация
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<RabbitMqService> _rabbitMqServiceMock;

    public GetTasksTests()
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
    #region GetTasks_ReturnsFromCache_WhenCacheHit

    /// <summary>
    /// Проверяет, что GetTasks возвращает данные из кэша когда они доступны.
    /// </summary>
    [Fact]
    public async Task GetTasks_ReturnsFromCache_WhenCacheHit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var cachedResult = new { total = 1, page = 1, pageSize = 10, items = new[] { new { Id = 1, Title = "Test" } } };
        string expectedCacheKey = "tasks_search=null_status=null_page=1_pageSize=10";

        _cacheServiceMock.Setup(x => x.GetAsync<object>(expectedCacheKey))
            .ReturnsAsync(cachedResult);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetTasks(null, null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(cachedResult, okResult.Value);
    }
    #endregion
    #region GetTasks_FiltersTasksByTitle_WhenSearchProvided
    /// <summary>
    /// Проверяет, что GetTasks корректно обрабатывает поиск по названию.
    /// </summary>
    [Fact]
    public async Task GetTasks_FiltersTasksByTitle_WhenSearchProvided()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var task1 = new Tasks { Id = 1, Title = "Important Task", CreatedAt = DateTime.UtcNow };
        var task2 = new Tasks { Id = 2, Title = "Regular Work", CreatedAt = DateTime.UtcNow };
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();

        _cacheServiceMock.Setup(x => x.GetAsync<object>(It.IsAny<string>()))
            .ReturnsAsync((object)null);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetTasks("important", null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        dynamic resultValue = okResult.Value;
        var items = Assert.IsAssignableFrom<IEnumerable<Tasks>>(resultValue.GetType().GetProperty("items").GetValue(resultValue));
        Assert.Single(items);
    }
    #endregion
    #region GetTasks_FiltersTasksByStatus_WhenStatusProvided
    /// <summary>
    /// Проверяет, что GetTasks корректно обрабатывает фильтрацию по статусу.
    /// </summary>
    [Fact]
    public async Task GetTasks_FiltersTasksByStatus_WhenStatusProvided()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var task1 = new Tasks { Id = 1, Title = "Task 1", Status = "pending", CreatedAt = DateTime.UtcNow };
        var task2 = new Tasks { Id = 2, Title = "Task 2", Status = "completed", CreatedAt = DateTime.UtcNow };
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();

        _cacheServiceMock.Setup(x => x.GetAsync<object>(It.IsAny<string>()))
            .ReturnsAsync((object)null);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetTasks(null, "pending", 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        dynamic resultValue = okResult.Value;
        var items = Assert.IsAssignableFrom<IEnumerable<Tasks>>(resultValue.GetType().GetProperty("items").GetValue(resultValue));
        Assert.Single(items);
    }
    #endregion
    #region GetTasks_NormalizesPageParameters_WhenInvalidValuesProvided
    /// <summary>
    /// Проверяет, что GetTasks корректно нормализует некорректные параметры пагинации.
    /// </summary>
    [Fact]
    public async Task GetTasks_NormalizesPageParameters_WhenInvalidValuesProvided()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _cacheServiceMock.Setup(x => x.GetAsync<object>(It.IsAny<string>()))
            .ReturnsAsync((object)null);

        var controller = new TasksController(context, _cacheMock.Object, _cacheServiceMock.Object, _rabbitMqServiceMock.Object);

        // Act
        var result = await controller.GetTasks(null, null, -1, -5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        dynamic resultValue = okResult.Value;
        Assert.Equal(1, resultValue.GetType().GetProperty("page").GetValue(resultValue));
        Assert.Equal(10, resultValue.GetType().GetProperty("pageSize").GetValue(resultValue));
    }

    #endregion
}
