using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualLibrary.Data;
using VirtualLibrary.Models;
using VirtualLibrary.ViewModels;

namespace VirtualLibrary.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<IdentityUser> _userManager;

        public BooksController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        { 
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        //GET: /Books
        public async Task<IActionResult> Index(string searchTitle, string searchAuthor, Genre? searchGenre, int page = 1, int pageSize = 5)
        {
            var booksQuery = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(searchTitle))
                booksQuery = booksQuery.Where(b => b.Title.Contains(searchTitle));

            if (!string.IsNullOrEmpty(searchAuthor))
                booksQuery = booksQuery.Where(b => b.Author.Contains(searchAuthor));

            if (searchGenre != null)
                booksQuery = booksQuery.Where(b => b.Genre == searchGenre);

            int totalBooks = await booksQuery.CountAsync();
            var books = await booksQuery
                .OrderBy(b => b.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalBooks / pageSize);
            ViewBag.SearchTitle = searchTitle;
            ViewBag.SearchAuthor = searchAuthor;
            ViewBag.SearchGenre = searchGenre;

            return View(books);
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return NotFound();

            return View(book);
        }


        //GET: /books/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        { 
            return View();
        }

        // POST: /Books/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookUploadViewModel model)
        {
            if (ModelState.IsValid)
            {
                string filePath = "";
                string? coverPath = null;

                if (model.File != null && model.File.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if(!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
                    filePath = Path.Combine("uploads", fileName);

                    using (var stream = new FileStream(Path.Combine(_env.WebRootPath, filePath), FileMode.Create))
                    { 
                        await model.File.CopyToAsync(stream);
                    }
                }

                // Save cover image if it exists
                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    var coverFolder = Path.Combine(_env.WebRootPath, "covers");
                    Directory.CreateDirectory(coverFolder);

                    var coverFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.CoverImage.FileName);
                    coverPath = Path.Combine("covers", coverFileName);

                    using (var stream = new FileStream(Path.Combine(_env.WebRootPath, coverPath), FileMode.Create))
                    {
                        await model.CoverImage.CopyToAsync(stream);
                    }
                }

                var user = await _userManager.GetUserAsync(User);

                var book = new Book
                {
                    Title = model.Title,
                    Author = model.Author,
                    Description = model.Description,
                    Genre = model.Genre,
                    FilePath = "/" + filePath.Replace("\\", "/"),
                    CoverImagePath = coverPath != null ? "/" + coverPath.Replace("\\", "/") : null,
                    UploadedByUserId = user?.Id
                };

                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();

            var model = new BookUploadViewModel
            {
                Title = book.Title,
                Author = book.Author,
                Description = book.Description,
                Genre = book.Genre
                // Not files
            };

            ViewData["BookId"] = book.Id;
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookUploadViewModel model)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                book.Title = model.Title;
                book.Author = model.Author;
                book.Description = model.Description;
                book.Genre = model.Genre;

                // Replace pdf if new one was uploaded
                if (model.File != null && model.File.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var newFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
                    var newFilePath = Path.Combine("uploads", newFileName);
                    var fullNewPath = Path.Combine(_env.WebRootPath, newFilePath);

                    // delete old PDF
                    var oldFile = Path.Combine(_env.WebRootPath, book.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFile))
                        System.IO.File.Delete(oldFile);

                    using (var stream = new FileStream(fullNewPath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }

                    book.FilePath = "/" + newFilePath.Replace("\\", "/");
                }

                // replace image if new one
                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    var coverFolder = Path.Combine(_env.WebRootPath, "covers");
                    Directory.CreateDirectory(coverFolder);

                    var newCoverName = Guid.NewGuid().ToString() + Path.GetExtension(model.CoverImage.FileName);
                    var newCoverPath = Path.Combine("covers", newCoverName);
                    var fullNewCoverPath = Path.Combine(_env.WebRootPath, newCoverPath);

                    // delete old cover
                    if (!string.IsNullOrEmpty(book.CoverImagePath))
                    {
                        var oldCover = Path.Combine(_env.WebRootPath, book.CoverImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldCover))
                            System.IO.File.Delete(oldCover);
                    }

                    using (var stream = new FileStream(fullNewCoverPath, FileMode.Create))
                    {
                        await model.CoverImage.CopyToAsync(stream);
                    }

                    book.CoverImagePath = "/" + newCoverPath.Replace("\\", "/");
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["BookId"] = id;
            return View(model);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();

            // Delete PDF file
            var filePath = Path.Combine(_env.WebRootPath, book.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            // Șterge coperta
            if (!string.IsNullOrEmpty(book.CoverImagePath))
            {
                var coverPath = Path.Combine(_env.WebRootPath, book.CoverImagePath.TrimStart('/'));
                if (System.IO.File.Exists(coverPath))
                    System.IO.File.Delete(coverPath);
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
