namespace Pm.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(string module, int? entityId, string action, int userId, string description);
    }
}