using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TechStoreWeb.Data;
using Microsoft.AspNetCore.Http;
using TechStoreWeb.Models;
using TechStoreWeb.Services;

namespace TechStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public EmployeesController(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            var employees = await _context.Users
                .Where(u => u.Role == "Admin" || u.Role == "Employee")
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
            
            return View(employees);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            if (id == null) return NotFound();

            var employee = await _context.Users.FindAsync(id);
            if (employee == null || (employee.Role != "Admin" && employee.Role != "Employee")) return NotFound();

            return View(employee);
        }

        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Username,Email,PhoneNumber,Password,Role")] User employee,
            string[]? SelectedPermissions)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });

            ModelState.Remove("LoginProvider");
            ModelState.Remove("ProviderKey");
            ModelState.Remove("Permissions");

            employee.Permissions = employee.Role == "Admin"
                ? null
                : AdminPermissions.Normalize(SelectedPermissions);

            if (ModelState.IsValid)
            {
                var exists = _context.Users.Any(u => u.Email == employee.Email || 
                    (employee.Username != null && u.Username == employee.Username));
                if (exists)
                {
                    ModelState.AddModelError("", "Email hoặc tên đăng nhập đã tồn tại.");
                    return View(employee);
                }

                employee.IsLocked = false;
                employee.LoginProvider = "Local";
                employee.Password = _passwordHasher.Hash(employee.Password!);
                _context.Users.Add(employee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            if (id == null) return NotFound();

            var employee = await _context.Users.FindAsync(id);
            if (employee == null || (employee.Role != "Admin" && employee.Role != "Employee")) return NotFound();

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Username,Email,PhoneNumber,Role")] User updatedEmployee,
            string? NewPassword, string[]? SelectedPermissions)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            if (id != updatedEmployee.UserId) return NotFound();

            ModelState.Remove("LoginProvider");
            ModelState.Remove("ProviderKey");
            ModelState.Remove("Password");
            ModelState.Remove("Permissions");

            updatedEmployee.Permissions = updatedEmployee.Role == "Admin"
                ? null
                : AdminPermissions.Normalize(SelectedPermissions);

            if (ModelState.IsValid)
            {
                var employee = await _context.Users.FindAsync(id);
                if (employee == null) return NotFound();

                var exists = _context.Users.Any(u => u.UserId != id && (u.Email == updatedEmployee.Email || 
                    (updatedEmployee.Username != null && u.Username == updatedEmployee.Username)));
                if (exists)
                {
                    ModelState.AddModelError("", "Email hoặc tên đăng nhập đã tồn tại.");
                    return View(updatedEmployee);
                }

                employee.FullName = updatedEmployee.FullName;
                employee.Username = updatedEmployee.Username;
                employee.Email = updatedEmployee.Email;
                employee.PhoneNumber = updatedEmployee.PhoneNumber;
                employee.Role = updatedEmployee.Role;
                employee.Permissions = updatedEmployee.Permissions;

                if (!string.IsNullOrEmpty(NewPassword))
                {
                    employee.Password = _passwordHasher.Hash(NewPassword);
                }

                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(updatedEmployee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });

            var employee = await _context.Users.FindAsync(id);
            if (employee != null)
            {
                if (employee.Role == "Admin")
                {
                    TempData["ErrorMessage"] = "Không thể khóa tài khoản Admin.";
                    return RedirectToAction(nameof(Index));
                }

                employee.IsLocked = !employee.IsLocked;
                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = employee.IsLocked ? "Đã khóa tài khoản nhân viên." : "Đã mở khóa tài khoản nhân viên.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
