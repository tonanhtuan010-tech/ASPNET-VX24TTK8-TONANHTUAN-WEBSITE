using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebsiteBanLaptop.Hubs;

[Authorize(Roles = "Admin")]
public sealed class AdminOrdersHub : Hub
{
}
