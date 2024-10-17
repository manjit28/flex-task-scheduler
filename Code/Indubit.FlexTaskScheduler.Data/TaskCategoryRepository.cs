using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Indubit.FlexTaskScheduler.Models;

namespace Indubit.FlexTaskScheduler.Data
{
    public class TaskCategoryRepository
    {
        private readonly TaskDbContext _context;

        public TaskCategoryRepository(TaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TaskCategory>> GetAllAsync()
        {
            return await _context.TaskCategories.ToListAsync();
        }

        public async Task<TaskCategory> GetByIdAsync(int id)
        {
            return await _context.TaskCategories.FindAsync(id);
        }

        public async Task AddAsync(TaskCategory taskCategory)
        {
            _context.TaskCategories.Add(taskCategory);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskCategory taskCategory)
        {
            _context.TaskCategories.Update(taskCategory);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var taskCategory = await _context.TaskCategories.FindAsync(id);
            if (taskCategory != null)
            {
                _context.TaskCategories.Remove(taskCategory);
                await _context.SaveChangesAsync();
            }
        }
    }
}
