using OrderProcessor.Models;
using OrderProcessor.Services;
using Xunit;

namespace OrderProcessor.Tests.Services;

/// <summary>
/// Intentionally slow-running tests that surface the three performance bugs
/// in <see cref="SalesReportGenerator.GenerateDetailedReport"/> when profiled:
/// <list type="bullet">
///   <item>Bug 1 — O(N²) string concatenation in the order loop</item>
///   <item>Bug 2 — O(N × K × M) linear scan of the product catalog per item</item>
///   <item>Bug 3 — Three separate LINQ passes to compute summary statistics</item>
/// </list>
/// Run these under the VS / Copilot profiler to observe the hot paths.
/// </summary>
public class SalesReportGeneratorTests
{
    // Tune these constants to make the profiler demo visually obvious.
    private const int OrderCount    = 2_000;
    private const int CatalogSize   = 500;
    private const int ItemsPerOrder = 10;

    [Fact]
    public void GenerateDetailedReport_LargeDataset_ReturnsWellFormedReport()
    {
        // Arrange — 2 000 orders × 10 items against a 500-product catalog
        var catalog = BuildProductCatalog(CatalogSize);
        var orders  = BuildOrders(OrderCount, catalog, ItemsPerOrder);
        var sut     = new SalesReportGenerator();

        // Act — O(N²) string alloc + O(N·K·M) product scan + 3× LINQ pass
        string report = sut.GenerateDetailedReport(orders, catalog);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("=== SALES REPORT ===", report);
        Assert.Contains($"Orders included: {OrderCount}", report);
        Assert.Contains("--- SUMMARY ---", report);
        Assert.Contains("Total Revenue", report);
    }

    [Fact]
    public void GenerateDetailedReport_AllOrdersCancelled_ShowsZeroAverageOrderValue()
    {
        // Arrange — forces the activeOrders == 0 branch in the summary
        var catalog = BuildProductCatalog(CatalogSize);
        var orders  = BuildOrders(OrderCount, catalog, ItemsPerOrder,
                                  forceStatus: OrderStatus.Cancelled);
        var sut = new SalesReportGenerator();

        // Act
        string report = sut.GenerateDetailedReport(orders, catalog);

        // Assert
        Assert.Contains("Active Orders          : 0", report);
        Assert.Contains("Average Order Value    : $0.00", report);
    }

    // ── test-data helpers ───────────────────────────────────────────────────

    private static List<Product> BuildProductCatalog(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Product
            {
                Id  = $"P{i:D4}",
                Sku = $"SKU-{i:D6}"
            })
            .ToList();

    private static List<Order> BuildOrders(
        int           count,
        List<Product> catalog,
        int           itemsPerOrder,
        OrderStatus?  forceStatus = null)
    {
        var rng = new Random(42);

        return Enumerable.Range(1, count)
            .Select(i =>
            {
                var items = Enumerable.Range(1, itemsPerOrder)
                    .Select(j =>
                    {
                        var product  = catalog[rng.Next(catalog.Count)];
                        int qty      = rng.Next(1, 10);
                        decimal unit = Math.Round((decimal)(j % 50 + 1) * 3.99m, 2);
                        return new OrderItem
                        {
                            ProductId   = product.Id,
                            ProductName = $"Sample Product {j}",
                            Quantity    = qty,
                            UnitPrice   = unit,
                        };
                    })
                    .ToList();

                decimal subTotal = items.Sum(it => it.LineTotal);
                decimal discount = Math.Round(subTotal * 0.05m, 2);
                decimal tax      = Math.Round((subTotal - discount) * 0.10m, 2);

                return new Order
                {
                    Id             = $"ORD-{i:D6}",
                    CustomerId     = $"CUST-{rng.Next(1, 300):D4}",
                    OrderDate      = DateTime.UtcNow.AddDays(-rng.Next(0, 365)),
                    Status         = forceStatus ?? (OrderStatus)(rng.Next(0, 3)),
                    Items          = items,
                    SubTotal       = subTotal,
                    DiscountAmount = discount,
                    TaxAmount      = tax,
                    Total          = subTotal - discount + tax
                };
            })
            .ToList();
    }
}