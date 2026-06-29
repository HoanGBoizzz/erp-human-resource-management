using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace QLNS_BE.Hubs
{
    /// <summary>
    /// SignalR Hub cho thông báo realtime
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Mapping userId -> connectionId để push targeted notifications
        /// </summary>
        private static readonly Dictionary<int, HashSet<string>> UserConnections = new();
        private static readonly object _lock = new();

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                lock (_lock)
                {
                    if (!UserConnections.ContainsKey(userId))
                        UserConnections[userId] = new HashSet<string>();
                    
                    UserConnections[userId].Add(Context.ConnectionId);
                }

                // Thêm user vào group với userId
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                lock (_lock)
                {
                    if (UserConnections.ContainsKey(userId))
                    {
                        UserConnections[userId].Remove(Context.ConnectionId);
                        if (UserConnections[userId].Count == 0)
                            UserConnections.Remove(userId);
                    }
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Lấy danh sách connectionIds của user
        /// </summary>
        public static IEnumerable<string> GetConnectionIds(int userId)
        {
            lock (_lock)
            {
                if (UserConnections.TryGetValue(userId, out var connections))
                    return connections.ToList();
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Kiểm tra user có online không
        /// </summary>
        public static bool IsUserOnline(int userId)
        {
            lock (_lock)
            {
                return UserConnections.ContainsKey(userId) && UserConnections[userId].Count > 0;
            }
        }

        private int GetUserId()
        {
            var claim = Context.User?.FindFirst("userid");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }
}
