using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStoreWeb.Data;
using TechStoreWeb.Models;

namespace TechStoreWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        private const int PageSize = 20;

        public async Task<IActionResult> Index(int? categoryId, string searchString, int page = 1)
        {
            ViewBag.Categories = await _context.Categories.Include(c => c.Products).ToListAsync();

            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                productsQuery = productsQuery.Where(p => EF.Functions.Like(p.Name, "%" + s + "%"));
                ViewData["SearchString"] = s;
            }

            var totalItems = await productsQuery.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var products = await productsQuery
                .OrderBy(p => p.ProductId)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var pageProductIds = products.Select(p => p.ProductId).ToList();
            ViewBag.RatingSummaries = await _context.Reviews
                .Where(r => pageProductIds.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new { ProductId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToDictionaryAsync(x => x.ProductId, x => new RatingSummary { Average = x.Average, Count = x.Count });

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.CategoryId = categoryId;

            return View(products);
        }

        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            var detail = await _context.ProductDetails
                .FirstOrDefaultAsync(d => d.Name == product.Name);

            ViewBag.Categories = await _context.Categories.Include(c => c.Products).ToListAsync();

            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == product.ProductId)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == product.ProductId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            bool canReview = false;
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId != null)
            {
                var user = await _context.Users.FindAsync(sessionUserId.Value);
                if (user != null)
                {
                    canReview = await _context.Orders
                        .AnyAsync(o => o.UserId == user.UserId
                                    && o.Status == "Delivered"
                                    && o.OrderDetails.Any(od => od.ProductId == product.ProductId));
                }
            }

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Detail = detail,
                Variants = variants,
                Reviews = reviews,
                CanReview = canReview
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null) return RedirectToAction("Login", "Account", new { returnUrl = $"/Home/Detail/{productId}" });

            var user = await _context.Users.FindAsync(sessionUserId.Value);
            if (user == null) return RedirectToAction("Login", "Account", new { returnUrl = $"/Home/Detail/{productId}" });

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToAction("Detail", new { id = productId });
            }

            bool canReview = await _context.Orders
                .AnyAsync(o => o.UserId == user.UserId
                            && o.Status == "Delivered"
                            && o.OrderDetails.Any(od => od.ProductId == productId));

            if (canReview)
            {
                var review = new Review
                {
                    ProductId = productId,
                    UserId = user.UserId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = System.DateTime.Now
                };
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
            }
            
            return RedirectToAction("Detail", new { id = productId });
        }
    }
}