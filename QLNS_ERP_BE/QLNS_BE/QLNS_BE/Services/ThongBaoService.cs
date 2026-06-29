using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Hubs;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    /// <summary>
    /// Dtos cho ThongBao
    /// </summary>
    public class ThongBaoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Message { get; set; }
        public string Type { get; set; } = "THONG_BAO";
        public string? RelatedEntity { get; set; }
        public int? RelatedId { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? SenderName { get; set; }
    }

    /// <summary>
    /// Service quản lý thông báo realtime
    /// </summary>
    public class ThongBaoService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ThongBaoService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Tạo thông báo mới và push realtime
        /// </summary>
        public async Task<ThongBao> CreateAndPushAsync(
            int userId,
            string title,
            string? message = null,
            string type = "THONG_BAO",
            string? relatedEntity = null,
            int? relatedId = null,
            string? link = null,
            int? senderId = null)
        {
            var thongBao = new ThongBao
            {
                UserId = userId,
                SenderId = senderId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntity = relatedEntity,
                RelatedId = relatedId,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();

            // Push realtime notification qua SignalR
            var dto = await GetDtoAsync(thongBao.Id);
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", dto);

            return thongBao;
        }

        /// <summary>
        /// Lấy danh sách thông báo chưa đọc của user
        /// </summary>
        public async Task<List<ThongBaoDto>> GetUnreadAsync(int userId, int limit = 20)
        {
            return await _context.ThongBaos
                .Where(x => x.UserId == userId && !x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .Select(x => new ThongBaoDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Message = x.Message,
                    Type = x.Type,
                    RelatedEntity = x.RelatedEntity,
                    RelatedId = x.RelatedId,
                    Link = x.Link,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt,
                    SenderName = x.Sender != null ? x.Sender.TenDangNhap : null
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tất cả thông báo của user (có phân trang)
        /// </summary>
        public async Task<List<ThongBaoDto>> GetAllAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.ThongBaos
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ThongBaoDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Message = x.Message,
                    Type = x.Type,
                    RelatedEntity = x.RelatedEntity,
                    RelatedId = x.RelatedId,
                    Link = x.Link,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt,
                    SenderName = x.Sender != null ? x.Sender.TenDangNhap : null
                })
                .ToListAsync();
        }

        /// <summary>
        /// Đếm số thông báo chưa đọc
        /// </summary>
        public async Task<int> CountUnreadAsync(int userId)
        {
            return await _context.ThongBaos
                .CountAsync(x => x.UserId == userId && !x.IsRead);
        }

        /// <summary>
        /// Đánh dấu đã đọc 1 thông báo
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int id, int userId)
        {
            var thongBao = await _context.ThongBaos
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (thongBao == null) return false;

            thongBao.IsRead = true;
            await _context.SaveChangesAsync();

            // Push update số lượng thông báo
            await PushUnreadCountAsync(userId);

            return true;
        }

        /// <summary>
        /// Đánh dấu tất cả đã đọc
        /// </summary>
        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var count = await _context.ThongBaos
                .Where(x => x.UserId == userId && !x.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));

            if (count > 0)
                await PushUnreadCountAsync(userId);

            return count;
        }

        /// <summary>
        /// Push số lượng thông báo chưa đọc qua SignalR
        /// </summary>
        private async Task PushUnreadCountAsync(int userId)
        {
            var count = await CountUnreadAsync(userId);
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("UpdateUnreadCount", count);
        }

        /// <summary>
        /// Lấy DTO của 1 thông báo
        /// </summary>
        private async Task<ThongBaoDto?> GetDtoAsync(int id)
        {
            return await _context.ThongBaos
                .Where(x => x.Id == id)
                .Select(x => new ThongBaoDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Message = x.Message,
                    Type = x.Type,
                    RelatedEntity = x.RelatedEntity,
                    RelatedId = x.RelatedId,
                    Link = x.Link,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt,
                    SenderName = x.Sender != null ? x.Sender.TenDangNhap : null
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Broadcast entity update to all connected clients (for general list refresh)
        /// </summary>        /// <summary>
        /// Gửi thông báo đến toàn bộ user (cho các sự kiện hệ thống)
        /// Không lưu vào DB để tránh spam bảng ThongBao, chỉ push realtime
        /// </summary>
        public async Task BroadcastToAllAsync(string title, string message, string type = "SYSTEM_BROADCAST")
        {
            var notification = new
            {
                Type = type,
                Title = title,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            // Gửi đến tất cả connected clients
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        }
        public async Task BroadcastEntityUpdateAsync(string entityType, int entityId, string action, object? data = null)
        {
            await _hubContext.Clients.All.SendAsync("EntityUpdated", new
            {
                entityType,
                entityId,
                action,
                data,
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Broadcast entity update to specific users
        /// </summary>
        public async Task BroadcastEntityUpdateToUsersAsync(IEnumerable<int> userIds, string entityType, int entityId, string action, object? data = null)
        {
            foreach (var userId in userIds)
            {
                await _hubContext.Clients.Group($"user_{userId}").SendAsync("EntityUpdated", new
                {
                    entityType,
                    entityId,
                    action,
                    data,
                    timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo liên quan đến entity đã đọc
        /// Dùng khi user navigate vào trang detail của entity
        /// </summary>
        public async Task<int> MarkAsReadByEntityAsync(int userId, string entityType, int entityId)
        {
            var count = await _context.ThongBaos
                .Where(x => x.UserId == userId
                    && !x.IsRead
                    && x.RelatedEntity == entityType
                    && x.RelatedId == entityId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));

            if (count > 0)
                await PushUnreadCountAsync(userId);

            return count;
        }
    }
}
