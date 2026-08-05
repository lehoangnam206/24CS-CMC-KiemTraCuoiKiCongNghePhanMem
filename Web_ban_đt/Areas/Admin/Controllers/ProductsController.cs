using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TechStoreWeb.Data;
using TechStoreWeb.Models;
using Microsoft.AspNetCore.Http;

namespace TechStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return TechStoreWeb.Services.AdminPermissions.Can(
                HttpContext, _context, TechStoreWeb.Services.AdminPermissions.Products);
        }

        private bool HasAccess() => IsAdmin();

        public async Task<IActionResult> Index()
        {
            if (!HasAccess()) return RedirectToAction("Login", "Account", new { area = "" });
            var products = await _context.Products.Include(p => p.Category).OrderByDescending(p => p.ProductId).ToListAsync();
            return View(products);
        }

        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Price,CostPrice,ImageUrl,CategoryId")] Product product)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });

            if (ModelState.IsValid)
            {
                if (_context.Products.Any(p => p.Name == product.Name))
                {
                    ModelState.AddModelError("Name", "Tên sản phẩm đã tồn tại.");
                    ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
                    return View(product);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Price,CostPrice,ImageUrl,CategoryId")] Product product)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Products.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Name = product.Name;
                    existing.Price = product.Price;
                    existing.CostPrice = product.CostPrice;
                    existing.ImageUrl = product.ImageUrl;
                    existing.CategoryId = product.CategoryId;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa sản phẩm.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
