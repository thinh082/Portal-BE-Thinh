using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
namespace Portal_BE.Config
{
        public class NotificationHub:Hub
        {
            public async Task JoinGroup(string groupName)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }
            public async Task LeaveGroup(string groupName)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }

            public override Task OnDisconnectedAsync(Exception? exception)
            {
                // nếu cần cleanup
                return base.OnDisconnectedAsync(exception);
            }
        } 
}
