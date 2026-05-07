using System.Security.Claims;
using CloudCompute.Data;
using CloudCompute.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.ViewComponents;

public class CreditBalanceViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public CreditBalanceViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var raw = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(raw, out var userId))
        {
            return View(new CreditBalanceViewModel { Balance = 0m });
        }

        var balance = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => (decimal?)u.CreditBalance)
            .FirstOrDefaultAsync() ?? 0m;

        return View(new CreditBalanceViewModel { Balance = balance });
    }
}
