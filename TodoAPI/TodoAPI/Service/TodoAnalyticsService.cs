using Grpc.Core;
using GrpcTodoService;
using Microsoft.EntityFrameworkCore;
using TodoAPI.Data;
namespace TodoAPI.Service;
public class TodoAnalyticsService : TodoAnalytics.TodoAnalyticsBase
{
    private readonly AppDbContext _dbContext;
    public TodoAnalyticsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public override async Task<StatsResponse> GetStats(StatsRequest request, ServerCallContext context)
    {
        var tasks = _dbContext.Tasks;

        int activeCount = await tasks.CountAsync(a => a.Status.Equals("active"));
        int completedCount = await tasks.CountAsync(a => a.Status.Equals("completed"));

        var response = new StatsResponse
        {
            ActiveTasks = activeCount,
            CompletedTasks = completedCount
        };

        return response;
    }

}
