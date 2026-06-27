using OrderProcessor.Models;
using System.Text;

namespace OrderProcessor.Services;

/// <summary>
/// Generates detailed and summary sales reports from a set of orders.
/// </summary>
public class SalesReportGenerator
{
    // SCENARIO 2 (PROFILING) — THREE PERFORMANCE BUGS:
    //
    // Bug 1 (line ~40): String concatenation inside a loop. For N orders this allocates
    //   O(N²) bytes of intermediate strings. Use StringBuilder instead.
    //
    // Bug 2 (line ~47): Linear scan of productCatalog inside a nested loop.
    //   Where(p => p.Id == item.ProductId) is O(M) per item; with K items per order
    //   across N orders the total cost is O(N × K × M). Build a Dictionary<string, Product>
    //   once before the loop for O(1) lookups.
    //
    // Bug 3 (lines ~61–63): Three separate LINQ passes over `orders` for Sum, Count, and Average.
    //   Each iterates the full collection. Collapse into a single aggregation pass.
    public string GenerateDetailedReport(IList<Order> orders, IList<Product> productCatalog)
    {
        // FIX 1: StringBuilder eliminates O(N²) string allocations in the order loop
        var sb = new StringBuilder();

        // FIX 2: Pre-build a dictionary for O(1) lookups instead of O(M) linear scan per item
        var productLookup = productCatalog.ToDictionary(p => p.Id);

        sb.Append("=== SALES REPORT ===\n");
        sb.Append($"Generated: {DateTime.UtcNow:O}\n");
        sb.Append($"Orders included: {orders.Count}\n\n");

        // FIX 3: Single-pass aggregation instead of three separate LINQ iterations
        decimal totalRevenue = 0m;
        int activeOrders = 0;
        decimal totalForAverage = 0m;

        foreach (var order in orders)
        {
            sb.Append($"Order #{order.Id,10} | {order.OrderDate:yyyy-MM-dd} | " +
                      $"Customer: {order.CustomerId,-12} | Status: {order.Status}\n");

            foreach (var item in order.Items)
            {
                productLookup.TryGetValue(item.ProductId, out var product);
                string sku = product?.Sku ?? "N/A";

                sb.Append($"  - {item.ProductName,-30} (SKU: {sku,-12}) " +
                          $"x{item.Quantity,3} @ ${item.UnitPrice,8:F2}  =  ${item.LineTotal,9:F2}\n");
            }

            sb.Append($"  {"",46}SubTotal:  ${order.SubTotal,9:F2}\n");
            sb.Append($"  {"",46}Discount: -${order.DiscountAmount,9:F2}\n");
            sb.Append($"  {"",46}Tax:       ${order.TaxAmount,9:F2}\n");
            sb.Append($"  {"",46}TOTAL:     ${order.Total,9:F2}\n\n");

            totalRevenue += order.Total;
            if (order.Status != OrderStatus.Cancelled)
            {
                activeOrders++;
                totalForAverage += order.Total;
            }
        }

        decimal avgOrderValue = activeOrders > 0 ? totalForAverage / activeOrders : 0m;

        sb.Append("--- SUMMARY ---\n");
        sb.Append($"Total Orders Processed : {orders.Count}\n");
        sb.Append($"Active Orders          : {activeOrders}\n");
        sb.Append($"Total Revenue          : ${totalRevenue:F2}\n");
        sb.Append($"Average Order Value    : ${avgOrderValue:F2}\n");

        return sb.ToString();
    }

    // Corrected implementation for reference (not used by the demo path)
    public string GenerateDetailedReportOptimized(IList<Order> orders, IList<Product> productCatalog)
    {
        var sb = new StringBuilder();
        var productLookup = productCatalog.ToDictionary(p => p.Id);

        sb.AppendLine("=== SALES REPORT ===");
        sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
        sb.AppendLine($"Orders included: {orders.Count}");
        sb.AppendLine();

        decimal totalRevenue = 0m;
        int activeOrders = 0;
        decimal totalForAverage = 0m;

        foreach (var order in orders)
        {
            sb.AppendLine($"Order #{order.Id,10} | {order.OrderDate:yyyy-MM-dd} | " +
                          $"Customer: {order.CustomerId,-12} | Status: {order.Status}");

            foreach (var item in order.Items)
            {
                productLookup.TryGetValue(item.ProductId, out var product);
                sb.AppendLine($"  - {item.ProductName,-30} (SKU: {product?.Sku ?? "N/A",-12}) " +
                              $"x{item.Quantity,3} @ ${item.UnitPrice,8:F2}  =  ${item.LineTotal,9:F2}");
            }

            sb.AppendLine($"  TOTAL: ${order.Total:F2}");
            sb.AppendLine();

            totalRevenue += order.Total;
            if (order.Status != OrderStatus.Cancelled)
            {
                activeOrders++;
                totalForAverage += order.Total;
            }
        }

        sb.AppendLine("--- SUMMARY ---");
        sb.AppendLine($"Total Revenue       : ${totalRevenue:F2}");
        sb.AppendLine($"Active Orders       : {activeOrders}");
        sb.AppendLine($"Average Order Value : ${(activeOrders > 0 ? totalForAverage / activeOrders : 0):F2}");

        return sb.ToString();
    }
}
