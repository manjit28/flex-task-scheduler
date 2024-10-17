using Microsoft.EntityFrameworkCore;
using Indubit.FlexTaskScheduler.Models;

namespace Indubit.FlexTaskScheduler.Data
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskCategory> TaskCategories { get; set; }
        public DbSet<TaskType> TaskTypes { get; set; }
        public DbSet<ScheduledTask> ScheduledTasks { get; set; }
        public DbSet<TaskExecutionHistory> TaskExecutionHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskCategory>()
                .HasMany(tc => tc.TaskTypes)
                .WithOne(tt => tt.TaskCategory)
                .HasForeignKey(tt => tt.TaskCategoryId);

            modelBuilder.Entity<TaskType>()
                .HasMany(tt => tt.ScheduledTasks)
                .WithOne(st => st.TaskType)
                .HasForeignKey(st => st.TaskTypeId);

            modelBuilder.Entity<TaskExecutionHistory>()
                .HasOne(teh => teh.ScheduledTask)
                .WithMany()
                .HasForeignKey(teh => teh.TaskId);
        }
    }
}
