using OrderProcessor.Models;
using System.Text;

namespace OrderProcessor.Services;

/// <summary>
/// Generates detailed and summary sales reports from a set of orders.
/// </summary>
public class SalesReportGenerator
{
    /// <summary>
    /// Generates a detailed sales report for the given orders and product catalog.
    /// </summary>
    /// <param name="orders"></param>
    /// <param name="productCatalog"></param>
    /// <returns></returns>
    public string GenerateDetailedReport(IList<Order> orders, IList<Product> productCatalog)
    {
        string report = "=== SALES REPORT ===\n";
        report += $"Generated: {DateTime.UtcNow:O}\n";
        report += $"Orders included: {orders.Count}\n\n";

        foreach (var order in orders)
        {
            report += $"Order #{order.Id,10} | {order.OrderDate:yyyy-MM-dd} | " +
                      $"Customer: {order.CustomerId,-12} | Status: {order.Status}\n";

            foreach (var item in order.Items)
            {
                var product = productCatalog.Where(p => p.Id == item.ProductId).FirstOrDefault();
                string sku = product?.Sku ?? "N/A";

                report += $"  - {item.ProductName,-30} (SKU: {sku,-12}) " +
                          $"x{item.Quantity,3} @ ${item.UnitPrice,8:F2}  =  ${item.LineTotal,9:F2}\n";
            }

            report += $"  {"",46}SubTotal:  ${order.SubTotal,9:F2}\n";
            report += $"  {"",46}Discount: -${order.DiscountAmount,9:F2}\n";
            report += $"  {"",46}Tax:       ${order.TaxAmount,9:F2}\n";
            report += $"  {"",46}TOTAL:     ${order.Total,9:F2}\n\n";
        }

        decimal totalRevenue = orders.Sum(o => o.Total);
        int activeOrders = orders.Count(o => o.Status != OrderStatus.Cancelled);
        decimal avgOrderValue = activeOrders > 0
            ? orders.Where(o => o.Status != OrderStatus.Cancelled).Average(o => o.Total)
            : 0m;

        report += "--- SUMMARY ---\n";
        report += $"Total Orders Processed : {orders.Count}\n";
        report += $"Active Orders          : {activeOrders}\n";
        report += $"Total Revenue          : ${totalRevenue:F2}\n";
        report += $"Average Order Value    : ${avgOrderValue:F2}\n";

        return report;
    }

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
