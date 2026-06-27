using OrderProcessor.Models;
using OrderProcessor.Services;

Console.WriteLine("=== Localhost Melbourne — BRK207 Demo: Order Processor ===");
Console.WriteLine();

// Sample product catalogue
var catalogue = new List<Product>
{
    new() { Id = "P001", Name = "Mechanical Keyboard",   Sku = "KB-MX-001",  Category = "Peripherals", UnitPrice = 149.99m, StockQuantity = 120 },
    new() { Id = "P002", Name = "Curved Monitor 27\"",   Sku = "MN-27C-002", Category = "Displays",    UnitPrice = 549.00m, StockQuantity = 45  },
    new() { Id = "P003", Name = "USB-C Hub 10-in-1",     Sku = "HB-UC-003",  Category = "Accessories", UnitPrice = 79.95m,  StockQuantity = 200 },
    new() { Id = "P004", Name = "Ergonomic Chair",        Sku = "CH-ERG-004", Category = "Furniture",   UnitPrice = 899.00m, StockQuantity = 30  },
    new() { Id = "P005", Name = "Noise-Cancel Headset",  Sku = "HS-NC-005",  Category = "Audio",       UnitPrice = 299.95m, StockQuantity = 75  },
};

// Sample orders for batch processing demo
var pendingOrders = new List<Order>
{
    new()
    {
        Id = "ORD-001001", CustomerId = "CUST-A", PromoCode = "SAVE10",
        Items =
        [
            new() { ProductId = "P001", ProductName = "Mechanical Keyboard", Quantity = 1, UnitPrice = 149.99m },
            new() { ProductId = "P003", ProductName = "USB-C Hub 10-in-1",   Quantity = 2, UnitPrice = 79.95m  },
        ]
    },
    new()
    {
        Id = "ORD-001002", CustomerId = "CUST-B",
        Items = [ new() { ProductId = "P002", ProductName = "Curved Monitor 27\"", Quantity = 1, UnitPrice = 549.00m } ]
    },
    new()
    {
        Id = "ORD-001003", CustomerId = "CUST-C", PromoCode = "SAVE20",
        Items = [ new() { ProductId = "P005", ProductName = "Noise-Cancel Headset", Quantity = 3, UnitPrice = 299.95m } ]
    },
    new()
    {
        Id = "ORD-001004", CustomerId = "CUST-D",
        Items = [ new() { ProductId = "P004", ProductName = "Ergonomic Chair", Quantity = 1, UnitPrice = 899.00m } ]
    },
    new()
    {
        Id = "ORD-001005", CustomerId = "CUST-E",
        Items = [ new() { ProductId = "P001", ProductName = "Mechanical Keyboard", Quantity = 4, UnitPrice = 149.99m } ]
    },
};

// --- SCENARIO 1: Batch Processing (has a bug — last order is always skipped) ---
Console.WriteLine("--- Scenario 1: Batch Processing ---");
var processor = new OrderBatchProcessor(new DiscountCalculator());
var results = processor.ProcessBatch(pendingOrders);
Console.WriteLine($"Orders submitted : {pendingOrders.Count}");
Console.WriteLine($"Orders processed : {results.Count}");
Console.WriteLine($"Orders skipped   : {pendingOrders.Count - results.Count}  <-- investigate this!");
Console.WriteLine();

foreach (var r in results)
    Console.WriteLine($"  Order #{r.OrderId}: {(r.Success ? $"OK  ${r.FinalTotal:F2}" : $"FAIL {r.ErrorMessage}")}");

Console.WriteLine();

// --- SCENARIO 2: Sales Report (has performance bugs) ---
Console.WriteLine("--- Scenario 2: Sales Report Generation ---");
var rng = new Random(42);
foreach (var order in pendingOrders)
{
    var r = results.FirstOrDefault(x => x.OrderId == order.Id);
    if (r?.Success == true)
    {
        order.SubTotal = order.Items.Sum(i => i.LineTotal);
        order.Total = r.FinalTotal;
        order.OrderDate = DateTime.UtcNow.AddDays(-rng.Next(1, 30));
        order.Status = OrderStatus.Processing;
    }
}

var reportGen = new SalesReportGenerator();
var processedOrders = pendingOrders.Where(o => o.Status == OrderStatus.Processing).ToList();
Console.WriteLine(reportGen.GenerateDetailedReport(processedOrders, catalogue));

// --- SCENARIO 3: Discount Calculation (has bugs tested by unit tests) ---
Console.WriteLine("--- Scenario 3: Discount Calculator ---");
var calc = new DiscountCalculator();

decimal bundleOf3 = calc.CalculateBundleDiscount(3, 149.99m);
Console.WriteLine($"Bundle discount for qty=3 @ $149.99: ${bundleOf3:F2}  (expected: $44.997, see tests)");

var discount = calc.CalculateTotalDiscount(200m, LoyaltyTier.Gold, "SAVE10");
Console.WriteLine($"Total discount on $200 (Gold 10% + SAVE10 10%): ${discount.TotalDiscount:F2}  (expected: $40.00)");

decimal tax = calc.CalculateTax(200m, 40m);
Console.WriteLine($"Tax on $200 subtotal with $40 discount: ${tax:F2}  (expected: $20.00)");

Console.WriteLine();
Console.WriteLine("Run 'dotnet test' to see the failing unit tests for Scenario 3.");
