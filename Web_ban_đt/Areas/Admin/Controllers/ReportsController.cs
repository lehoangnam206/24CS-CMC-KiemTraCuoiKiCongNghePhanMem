using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TechStoreWeb.Data;
using Microsoft.AspNetCore.Http;
using TechStoreWeb.Models;
using System.Collections.Generic;

namespace TechStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        private const decimal EstimatedCostRatio = 0.7385m;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin" || role == "Employee";
        }

        public async Task<IActionResult> Index(string range = "month")
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });

            var vm = new ReportsDashboardViewModel { Range = range };

            var now = DateTime.Now;
            DateTime? from = range switch
            {
                "today" => now.Date,
                "7days" => now.Date.AddDays(-6),
                "year" => new DateTime(now.Year, 1, 1),
                "all" => null,
                _ => new DateTime(now.Year, now.Month, 1)
            };
            vm.RangeLabel = range switch
            {
                "today" => "Hôm nay",
                "7days" => "7 ngày",
                "year" => "Năm này",
                "all" => "Tất cả",
                _ => "Tháng này"
            };
            vm.FromDate = from;
            vm.ToDate = now;

            if (!await _context.Orders.AnyAsync())
            {
                vm.Message = "Chưa có thông tin để báo cáo.";
                return View(vm);
            }

            var ordersQuery = _context.Orders.Include(o => o.User).AsQueryable();
            if (from.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= from.Value);

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (orders.Count == 0)
            {
                vm.Message = $"Chưa có đơn hàng nào trong khoảng thời gian \"{vm.RangeLabel}\".";
                return View(vm);
            }

            vm.HasData = true;

            var activeOrders = orders.Where(o => o.Status != "Cancelled").ToList();

            var activeOrderIds = activeOrders.Select(o => o.OrderId).ToList();
            var soldItems = await _context.OrderDetails
                .Where(d => activeOrderIds.Contains(d.OrderId))
                .Include(d => d.Product).ThenInclude(p => p.Category)
                .ToListAsync();

            vm.TotalOrders = orders.Count;
            vm.TotalRevenue = activeOrders.Sum(o => o.TotalAmount);
            vm.AverageOrderValue = activeOrders.Count > 0
                ? Math.Round(vm.TotalRevenue / activeOrders.Count, 0)
                : 0;

            decimal profit = 0, lineRevenue = 0, revenueWithRealCost = 0;
            foreach (var item in soldItems)
            {
                var revenue = item.UnitPrice * item.Quantity;
                lineRevenue += revenue;

                var unitCost = item.Product?.CostPrice;
                if (unitCost.HasValue)
                {
                    profit += revenue - unitCost.Value * item.Quantity;
                    revenueWithRealCost += revenue;
                }
                else
                {
                    profit += revenue * (1 - EstimatedCostRatio);
                }
            }
            var uncoveredRevenue = vm.TotalRevenue - lineRevenue;
            if (uncoveredRevenue > 0)
                profit += uncoveredRevenue * (1 - EstimatedCostRatio);

            vm.EstimatedProfit = Math.Round(profit, 0);
            vm.CostCoveragePercent = vm.TotalRevenue > 0
                ? Math.Round((double)(revenueWithRealCost / vm.TotalRevenue) * 100, 1)
                : 0;
            vm.ProfitIsEstimated = vm.CostCoveragePercent < 99.9;

            vm.PendingOrders = orders.Count(o => o.Status == "Pending");
            vm.DeliveredOrders = orders.Count(o => o.Status == "Delivered");
            vm.CancelledOrders = orders.Count(o => o.Status == "Cancelled");
            vm.ProcessingOrders = orders.Count - vm.PendingOrders - vm.DeliveredOrders - vm.CancelledOrders;
            vm.CancelRate = vm.TotalOrders > 0
                ? Math.Round(vm.CancelledOrders * 100.0 / vm.TotalOrders, 1)
                : 0;

            foreach (var order in activeOrders)
            {
                int index = ((int)order.OrderDate.DayOfWeek + 6) % 7;
                vm.OrdersByDayOfWeek[index]++;
                vm.RevenueByDayOfWeek[index] += order.TotalAmount;
            }

            var soldStats = soldItems
                .Where(d => d.Product != null)
                .GroupBy(d => d.ProductId)
                .Select(g => new ProductSalesStat
                {
                    ProductId = g.Key,
                    Name = g.First().Product.Name,
                    BrandName = g.First().Product.Category?.CategoryName ?? "Khác",
                    ImageUrl = g.First().Product.ImageUrl,
                    QuantitySold = g.Sum(d => d.Quantity),
                    Revenue = g.Sum(d => d.UnitPrice * d.Quantity),
                    Stock = g.First().Product.Stock
                })
                .ToList();

            vm.TopProducts = soldStats
                .OrderByDescending(p => p.QuantitySold)
                .ThenByDescending(p => p.Revenue)
                .Take(10)
                .ToList();

            var brandTotals = soldStats
                .GroupBy(p => p.BrandName)
                .Select(g => new BrandSalesStat
                {
                    BrandName = g.Key,
                    Revenue = g.Sum(p => p.Revenue),
                    QuantitySold = g.Sum(p => p.QuantitySold)
                })
                .OrderByDescending(b => b.Revenue)
                .ToList();

            var maxBrandRevenue = brandTotals.Count > 0 ? brandTotals.Max(b => b.Revenue) : 0;
            foreach (var brand in brandTotals)
            {
                brand.Percent = maxBrandRevenue > 0
                    ? Math.Round((double)(brand.Revenue / maxBrandRevenue) * 100, 1)
                    : 0;
            }
            vm.BrandSales = brandTotals;

            var soldQuantities = soldStats.ToDictionary(p => p.ProductId, p => p);
            var allProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock > 0)
                .ToListAsync();

            vm.SlowProducts = allProducts
                .Select(p => new ProductSalesStat
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    BrandName = p.Category?.CategoryName ?? "Khác",
                    ImageUrl = p.ImageUrl,
                    QuantitySold = soldQuantities.TryGetValue(p.ProductId, out var s) ? s.QuantitySold : 0,
                    Revenue = soldQuantities.TryGetValue(p.ProductId, out var s2) ? s2.Revenue : 0,
                    Stock = p.Stock
                })
                .OrderBy(p => p.QuantitySold)
                .ThenByDescending(p => p.Stock)
                .Take(10)
                .ToList();

            vm.LowStockProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .Select(p => new LowStockProduct
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    BrandName = p.Category != null ? p.Category.CategoryName : "Khác",
                    Stock = p.Stock
                })
                .ToListAsync();

            vm.RecentOrders = orders;

            return View(vm);
        }

        public IActionResult Print()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account", new { area = "" });
            return RedirectToAction(nameof(Index));
        }
    }
}
