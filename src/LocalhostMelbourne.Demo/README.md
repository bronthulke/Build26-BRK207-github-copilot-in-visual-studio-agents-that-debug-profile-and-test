# Localhost Melbourne — BRK207 Demo: Order Processor

A realistic .NET 8 order-processing app with **deliberate bugs and performance issues** baked in,
designed to demonstrate the three GitHub Copilot agent workflows from BRK207:

1. [Scenario 1 — Debugging](#scenario-1-debugging)
2. [Scenario 2 — Profiling / Performance](#scenario-2-profiling--performance)
3. [Scenario 3 — Testing](#scenario-3-testing)

---

## Quick start

```bash
# Build
dotnet build

# Run the demo app (shows all three scenarios)
dotnet run --project OrderProcessor

# Run unit tests (10 pass, 10 fail by design)
dotnet test
```

---

## Project structure

```
LocalhostMelbourne.Demo/
├── OrderProcessor/               # Main console app
│   ├── Models/
│   │   ├── Customer.cs
│   │   ├── Order.cs              (Order, OrderItem, OrderDiscount, OrderResult)
│   │   └── Product.cs
│   └── Services/
│       ├── DiscountCalculator.cs  ← SCENARIO 3 bugs (testing)
│       ├── OrderBatchProcessor.cs ← SCENARIO 1 bug (debugging)
│       └── SalesReportGenerator.cs ← SCENARIO 2 bugs (profiling)
└── OrderProcessor.Tests/
    ├── DiscountCalculatorTests.cs  (12 tests: 6 pass, 6 fail)
    └── OrderBatchProcessorTests.cs (8 tests: 4 pass, 4 fail)
```

---

## Scenario 1 — Debugging

**File:** `OrderProcessor/Services/OrderBatchProcessor.cs`  
**Method:** `ProcessBatch(IList<Order> orders)` — line 33

### The bug

```csharp
// BUG: loop condition is orders.Count - 1 instead of orders.Count
for (int i = 0; i < orders.Count - 1; i++)
```

The nightly fulfilment pipeline silently drops the **last order in every batch**.
With a batch of 5 orders, only 4 are processed. With a batch of 1 order, nothing is processed.

The symptom surfaces in production as a gradually growing gap between orders submitted and orders
shipped — but no errors are logged because the loop doesn't throw.

### Copilot prompt to use

> "I'm submitting orders for processing but not all of them make it through. The count of processed
> orders is always one less than what I submitted. Open `OrderBatchProcessor.ProcessBatch` and
> investigate why orders are being skipped."

### Fix (one character)

```csharp
for (int i = 0; i < orders.Count; i++)   // remove the - 1
```

---

## Scenario 2 — Profiling / Performance

**File:** `OrderProcessor/Services/SalesReportGenerator.cs`  
**Method:** `GenerateDetailedReport(IList<Order> orders, IList<Product> productCatalog)` — line 28

### The bugs (three separate bottlenecks)

| # | Location | Issue | Cost |
|---|----------|-------|------|
| 1 | ~line 40 | `string report += …` inside a loop | O(N²) string allocations |
| 2 | ~line 47 | `productCatalog.Where(p => p.Id == …)` inside a nested loop | O(N × K × M) LINQ scans |
| 3 | ~lines 61–63 | Three separate `.Sum()`, `.Count()`, `.Average()` passes | 3× full iteration |

With a few hundred orders the report is visibly slow; with thousands it can time out.

### Copilot prompt to use

> "The sales report is slow — it takes several seconds to generate for just 500 orders. 
> Open the profiler trace for `GenerateDetailedReport` and help me understand where the 
> time is being spent and how to fix it."

### Reference fix

`GenerateDetailedReportOptimized` in the same file shows the corrected implementation
using `StringBuilder`, a pre-built `Dictionary<string, Product>` lookup, and a single
aggregation loop.

---

## Scenario 3 — Testing

**File:** `OrderProcessor/Services/DiscountCalculator.cs`  
**Test file:** `OrderProcessor.Tests/DiscountCalculatorTests.cs`

### The bugs (three in the implementation)

#### Bug 3a — Bundle discount off-by-one (`CalculateBundleDiscount`, line 33)

```csharp
if (quantity > 3)   // BUG: should be >= 3
```

Spec: "Buy **3 or more** units, get 10% off." Code: "Buy **more than 3**."  
Customers ordering exactly 3 units never get the discount.

Failing tests: `BundleDiscount_TenPercentForExactlyThreeUnits`, `BundleDiscount_ThreeKeyboardsGetTenPercent`

#### Bug 3b — Promo code stacked incorrectly (`CalculateTotalDiscount`, line 43)

```csharp
decimal remainingAfterLoyalty = subtotal - loyaltyDiscount;
decimal promoDiscount = remainingAfterLoyalty * promoRate;   // BUG: should be subtotal * promoRate
```

Spec: both loyalty and promo discounts are independent percentages of the **original subtotal**.  
Bug: promo is applied to the post-loyalty-discount amount, so customers are under-discounted.

Failing tests: `TotalDiscount_GoldLoyaltyPlusSave10Promo`, `TotalDiscount_PlatinumPlusSave20Promo`

#### Bug 3c — Tax base is wrong (`CalculateTax`, line 55)

```csharp
return (subtotal - discountAmount) * taxRate;   // BUG: should be subtotal * taxRate
```

Spec: GST is 10% of the **original subtotal** (before discounts).  
Bug: tax is calculated on the discounted amount, under-charging GST when discounts apply.

Failing tests: `Tax_CalculatedOnSubtotalNotDiscountedAmount`, `Tax_LargeDiscountDoesNotReduceTaxBase`

### Copilot prompt to use

> "Several unit tests in `DiscountCalculatorTests` are failing. Investigate the test failures,
> explain the root cause of each one, and suggest the minimal code change needed to fix the
> implementation in `DiscountCalculator`."

---

## Expected test results

```
Total tests: 20
     Passed: 10
     Failed: 10   ← these are the deliberate bugs
```

After applying all three fixes, all 20 tests should pass.
