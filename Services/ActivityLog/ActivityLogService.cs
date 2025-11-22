using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.Models;

namespace Pm.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;

        public ActivityLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string module, int? entityId, string action, int userId, string description)
        {
            var log = new ActivityLog
            {
                Module = module,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}