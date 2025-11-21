using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public interface IInspeksiTemuanKpcService
    {
        Task<PagedResultDto<InspeksiTemuanKpcDto>> GetAllAsync(InspeksiTemuanKpcQueryDto query);
        Task<InspeksiTemuanKpcDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateInspeksiTemuanKpcDto dto, int userId);
        Task<InspeksiTemuanKpcDto?> UpdateAsync(int id, UpdateInspeksiTemuanKpcDto dto, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<bool> RestoreAsync(int id, int userId);
         Task<bool> DeletePermanentAsync(int id, int userId);
        Task<byte[]> ExportToExcelAsync(bool history, DateTime? start, DateTime? end, string? ruang, string? status);
    }
}