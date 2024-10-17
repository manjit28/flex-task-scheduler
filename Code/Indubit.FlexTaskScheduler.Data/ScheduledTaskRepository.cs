using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Indubit.FlexTaskScheduler.Models;

namespace Indubit.FlexTaskScheduler.Data
{
    public class ScheduledTaskRepository
    {
        private readonly TaskDbContext _context;

        public ScheduledTaskRepository(TaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ScheduledTask>> GetAllAsync()
        {
            return await _context.ScheduledTasks
                .Include(st => st.TaskType)
                .ThenInclude(tt => tt.TaskCategory)
                .ToListAsync();
        }

        public async Task<ScheduledTask?> GetByIdAsync(int id)
        {
            return await _context.ScheduledTasks
                .Include(st => st.TaskType)
                .ThenInclude(tt => tt.TaskCategory)
                .FirstOrDefaultAsync(st => st.Id == id);
        }

        public async Task AddAsync(ScheduledTask scheduledTask)
        {
            _context.ScheduledTasks.Add(scheduledTask);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ScheduledTask scheduledTask)
        {
            _context.ScheduledTasks.Update(scheduledTask);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var scheduledTask = await _context.ScheduledTasks.FindAsync(id);
            if (scheduledTask != null)
            {
                _context.ScheduledTasks.Remove(scheduledTask);
                await _context.SaveChangesAsync();
            }
        }
    }
}