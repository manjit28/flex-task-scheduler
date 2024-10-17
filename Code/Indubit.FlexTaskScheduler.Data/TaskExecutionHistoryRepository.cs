using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Indubit.FlexTaskScheduler.Models;

namespace Indubit.FlexTaskScheduler.Data
{
    public class TaskExecutionHistoryRepository
    {
        private readonly TaskDbContext _context;

        public TaskExecutionHistoryRepository(TaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TaskExecutionHistory>> GetAllAsync()
        {
            return await _context.TaskExecutionHistories
                .Include(teh => teh.ScheduledTask)
                .ToListAsync();
        }

        public async Task<TaskExecutionHistory?> GetByIdAsync(int id)
        {
            return await _context.TaskExecutionHistories
                .Include(teh => teh.ScheduledTask)
                .FirstOrDefaultAsync(teh => teh.Id == id);
        }

        public async Task AddAsync(TaskExecutionHistory taskExecutionHistory)
        {
            _context.TaskExecutionHistories.Add(taskExecutionHistory);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskExecutionHistory taskExecutionHistory)
        {
            _context.TaskExecutionHistories.Update(taskExecutionHistory);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var taskExecutionHistory = await _context.TaskExecutionHistories.FindAsync(id);
            if (taskExecutionHistory != null)
            {
                _context.TaskExecutionHistories.Remove(taskExecutionHistory);
                await _context.SaveChangesAsync();
            }
        }
    }
}
