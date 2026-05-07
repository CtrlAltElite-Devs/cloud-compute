using System.Security.Claims;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Owner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

[Authorize(Roles = nameof(UserRole.Member))]
[Route("owner")]
public class OwnerController : Controller
{
    private readonly IOwnerEarningsService _earningsService;

    public OwnerController(IOwnerEarningsService earningsService)
    {
        _earningsService = earningsService;
    }

    [HttpGet("earnings")]
    public async Task<IActionResult> Earnings()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _earningsService.GetEarningsAsync(userId.Value);
        if (!model.IsOwnerVerified)
        {
            return Forbid();
        }

        return View(model);
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
