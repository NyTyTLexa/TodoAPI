using Microsoft.AspNetCore.Mvc;
using System;
using TodoAPI.Models;
using TodoAPI.Data;
using TodoAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TodoAPI.Service;
using System.Threading.Tasks;
using TodoAPI.Interface;
using TodoAPI.Repository;

namespace TodoAPI.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ICacheService _cacheService;
        private readonly RabbitMqService _rabbitMqService;
        private readonly SQLTaskRepository _taskRepository;
        public TasksController(AppDbContext context,IDistributedCache cache,
            ICacheService cacheService, RabbitMqService rabbitMqService)
        {
            _context = context;
            _cache = cache;
            _cacheService = cacheService;
            _rabbitMqService = rabbitMqService;
            _taskRepository = new SQLTaskRepository(context);
        }

        // GET: api/tasks/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Tasks>>> GetAllTasks()
        {
            string cacheKey = "all_tasks";
            var cachedTasks = await _cacheService.GetAsync<List<Tasks>>(cacheKey);

            if (cachedTasks != null)
                return Ok(cachedTasks);

            var tasks = await GetAllTasks();
            await _cacheService.SetAsync(cacheKey, tasks, TimeSpan.FromMinutes(5));

            return Ok(tasks);
        }

        // GET: api/tasks
        // Поиск, фильтрация, пагинация
        [HttpGet]
        public async Task<ActionResult<List<Tasks>>> GetTasks(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            string cacheKey = $"tasks_search={search ?? "null"}" +
                $"_status={status ?? "null"}_page={page}_pageSize={pageSize}";

            var cachedResult = await _cacheService.GetAsync<object>(cacheKey);
            if (cachedResult != null)
            {
                return Ok(cachedResult);
            }
            var items = await _taskRepository.GetPaginationTasks(search, status, page, pageSize);

            var result = new
            {
                page,
                pageSize,
                items
            };
            await _cacheService.SetAsync(cacheKey, result,
                TimeSpan.FromMinutes(5));

            return Ok();
        }


        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Tasks>> GetTask(int id)
        {
            string cacheKey = $"task_{id}";
            var cachedTasks = await _cacheService.GetAsync<Tasks>(cacheKey);

            if (cachedTasks != null)
                return Ok(cachedTasks);

            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) return NotFound();
            await _cacheService.SetAsync(cacheKey, task,
                TimeSpan.FromMinutes(5));

            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<Tasks>> CreateTask(Tasks task)
        {
            
            task.CreatedAt = DateTime.UtcNow;
            await _taskRepository.CreateAsync(task);

            await _cache.RemoveAsync("all_tasks");

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, Tasks task)
        {

            if (id != task.Id) return BadRequest();
            await _taskRepository.UpdateAsync(task);
            _rabbitMqService.Publish("task.status.changed", new
            {
                TaskId = task.Id,
                NewStatus = task.Status
            });
            await _cache.RemoveAsync("all_tasks");

            return NoContent();
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();
            await _taskRepository.DeleteAsync(task);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("all_tasks");
            return NoContent();
        }
    }

