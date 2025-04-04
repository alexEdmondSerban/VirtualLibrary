using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualLibrary.Data;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        var recentBooks = await _context.Books
            .Where(b => b.UploadDate >= firstDayOfMonth)
            .OrderByDescending(b => b.UploadDate)
            .ToListAsync();

        return View(recentBooks);
    }
}
