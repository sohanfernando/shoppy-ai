using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Models.DTOs.Notification;
using AdvancedOrderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedOrderSystem.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = AuthConstants.SignedInPolicy)]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationListResponse>> GetMine()
    {
        return Ok(await _notificationService.GetMineAsync(User));
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(User, id);

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(User);

        return NoContent();
    }
}
