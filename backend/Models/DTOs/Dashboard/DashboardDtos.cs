namespace AdvancedOrderSystem.Models.DTOs.Dashboard;

public class CustomerDashboardResponse
{
    public int TotalOrders { get; set; }

    public int ConfirmedOrders { get; set; }

    public int CancelledOrders { get; set; }

    // Total of confirmed orders only
    public decimal TotalPaid { get; set; }

    public decimal TotalSaved { get; set; }

    public decimal AverageOrderValue { get; set; }

    public int ItemsOrdered { get; set; }

    public DateTime? LastOrderDate { get; set; }

    // Chart 1: what was paid each month
    public List<MonthlySpendPoint> MonthlySpend { get; set; } = new();

    // Chart 2: where the money went
    public List<ProductSpendSlice> SpendByProduct { get; set; } = new();

    public List<RecentOrderSummary> RecentOrders { get; set; } = new();
}

public class MonthlySpendPoint
{
    // First day of the month, so the frontend can format it
    public DateTime Month { get; set; }

    public decimal Total { get; set; }

    public int OrderCount { get; set; }
}

public class ProductSpendSlice
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int Quantity { get; set; }
}

public class RecentOrderSummary
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ItemCount { get; set; }
}
