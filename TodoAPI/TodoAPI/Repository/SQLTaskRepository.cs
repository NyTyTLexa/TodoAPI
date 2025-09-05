using Grpc.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoAPI.Data;
using TodoAPI.Interface;
using TodoAPI.Models;
using TodoAPI.Service;
using static Humanizer.On;

namespace TodoAPI.Repository;

    public class SQLTaskRepository:IRepository<Tasks>
    {
        private readonly AppDbContext _context;
        private readonly RabbitMqService _rabbitMqService;
        public SQLTaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tasks>>GetPaginationTasks(string search,string status,int page, int pageSize)
        {
            var query = _context.Tasks.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Title.Equals(search.ToLower()));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status.Equals(status.ToLower()));
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
            .ToListAsync();

            return items;
        }
        public async Task<List<Tasks>> GetAllAsync()
        {
           return await _context.Tasks.ToListAsync();
        }
        public async Task<Tasks?> GetByIdAsync(int id)
        {
            return await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        }
        public async Task CreateAsync(Tasks task)
        {
            await _context.Tasks.AddAsync(task);
            await SaveChangesAsync();
        }
        public async Task UpdateAsync(Tasks task)
        {

            var existing = await _context.Tasks.FindAsync(task.Id);


            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Status = task.Status;

            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Tasks task)
        {
            _context.Tasks.Remove(task);
            await SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

