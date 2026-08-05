using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using TechStoreWeb.Data;

namespace TechStoreWeb.Services
{
    public static class AdminPermissions
    {
        public const string Orders = "Orders";
        public const string Customers = "Customers";
        public const string Promotions = "Promotions";
        public const string Products = "Products";

        public static readonly (string Key, string Label, string Icon)[] All =
        {
            (Orders,     "Quản lý Đơn hàng",    "bi-box-seam"),
            (Customers,  "Quản lý Khách hàng",  "bi-people"),
            (Promotions, "Quản lý Mã giảm giá", "bi-tags"),
            (Products,   "Quản lý Sản phẩm",    "bi-phone")
        };

        public static string LabelOf(string key) =>
            All.FirstOrDefault(p => p.Key == key).Label ?? key;

        public static bool Contains(string? permissions, string permission)
        {
            if (string.IsNullOrWhiteSpace(permissions)) return false;

            return permissions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
        }

        public static bool Can(HttpContext http, AppDbContext context, string permission)
        {
            var role = http.Session.GetString("Role");
            if (role == "Admin") return true;
            if (role != "Employee") return false;

            var userId = http.Session.GetInt32("UserId");
            if (userId == null) return false;

            var permissions = context.Users
                .Where(u => u.UserId == userId.Value && !u.IsLocked)
                .Select(u => u.Permissions)
                .FirstOrDefault();

            return Contains(permissions, permission);
        }

        public static string? Normalize(string[]? selected)
        {
            if (selected == null) return null;

            var valid = selected
                .Where(s => All.Any(p => string.Equals(p.Key, s, StringComparison.OrdinalIgnoreCase)))
                .Distinct()
                .ToArray();

            return valid.Length == 0 ? null : string.Join(",", valid);
        }
    }
}
