using System;
using System.Collections.Generic;

namespace TechStoreWeb.Models
{
    public class ReportsDashboardViewModel
    {
        public string Range { get; set; } = "month";
        public string RangeLabel { get; set; } = "Tháng này";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal EstimatedProfit { get; set; }
        public bool ProfitIsEstimated { get; set; }
        public double CostCoveragePercent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalOrders { get; set; }
        public double CancelRate { get; set; }

        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        public int[] OrdersByDayOfWeek { get; set; } = new int[7];
        public decimal[] RevenueByDayOfWeek { get; set; } = new decimal[7];

        public List<ProductSalesStat> TopProducts { get; set; } = new();
        public List<ProductSalesStat> SlowProducts { get; set; } = new();
        public List<BrandSalesStat> BrandSales { get; set; } = new();
        public List<LowStockProduct> LowStockProducts { get; set; } = new();

        public List<Order> RecentOrders { get; set; } = new();

        public bool HasData { get; set; }
        public string Message { get; set; }
    }

    public class ProductSalesStat
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string BrandName { get; set; }
        public string ImageUrl { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int Stock { get; set; }
    }

    public class BrandSalesStat
    {
        public string BrandName { get; set; }
        public decimal Revenue { get; set; }
        public int QuantitySold { get; set; }
        public double Percent { get; set; }
    }

    public class LowStockProduct
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string BrandName { get; set; }
        public int Stock { get; set; }
    }
}
