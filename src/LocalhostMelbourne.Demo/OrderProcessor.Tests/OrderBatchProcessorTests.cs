using OrderProcessor.Models;
using OrderProcessor.Services;
using Xunit;

namespace OrderProcessor.Tests;

/// <summary>
/// SCENARIO 1 (DEBUGGING): Tests for OrderBatchProcessor.
/// These tests catch the off-by-one bug in ProcessBatch — the last order is always skipped.
///
/// Copilot prompt: "These batch processor tests are failing. The symptom is that fewer
/// orders are returned than submitted. Investigate ProcessBatch and find the root cause."
/// </summary>
public class OrderBatchProcessorTests
{
    private readonly OrderBatchProcessor _processor = new(new DiscountCalculator());

    private static Order MakeOrder(int id, decimal unitPrice = 100m) => new()
    {
        Id = $"ORD-{id:D6}",
        CustomerId = $"CUST-{id}",
        Items = [ new() { ProductId = "P001", ProductName = "Widget", Quantity = 1, UnitPrice = unitPrice } ]
    };

    [Fact]  // FAILS: bug causes 0 orders to be processed when only 1 is submitted
    public void ProcessBatch_SingleOrder_ReturnsOneResult()
    {
        var orders = new List<Order> { MakeOrder(1) };
        var results = _processor.ProcessBatch(orders);
        Assert.Single(results);
    }

    [Fact]  // FAILS: 2 submitted → 1 processed (last skipped)
    public void ProcessBatch_TwoOrders_ReturnsTwoResults()
    {
        var orders = new List<Order> { MakeOrder(1), MakeOrder(2) };
        var results = _processor.ProcessBatch(orders);
        Assert.Equal(2, results.Count);
    }

    [Fact]  // FAILS: 5 submitted → 4 processed
    public void ProcessBatch_FiveOrders_ReturnsFiveResults()
    {
        var orders = Enumerable.Range(1, 5).Select(i => MakeOrder(i)).ToList();
        var results = _processor.ProcessBatch(orders);
        Assert.Equal(5, results.Count);
    }

    [Fact]  // FAILS: last order ID is never in the results
    public void ProcessBatch_AllOrderIdsArePresent()
    {
        var orders = Enumerable.Range(100, 10).Select(i => MakeOrder(i)).ToList();
        var results = _processor.ProcessBatch(orders);

        var resultIds = results.Select(r => r.OrderId).ToHashSet();
        foreach (var order in orders)
            Assert.Contains(order.Id, resultIds);
    }

    [Fact]
    public void ProcessBatch_EmptyList_ReturnsEmpty()
    {
        var results = _processor.ProcessBatch(new List<Order>());
        Assert.Empty(results);
    }

    [Fact]
    public void ProcessBatch_OrderWithNoItems_ReturnsFailure()
    {
        var emptyOrder = new Order { Id = "ORD-000099", CustomerId = "CUST-X", Items = [] };
        var orders = new List<Order> { MakeOrder(1), emptyOrder, MakeOrder(2) };
        var results = _processor.ProcessBatch(orders);

        var emptyResult = results.FirstOrDefault(r => r.OrderId == "ORD-000099");
        // Only verifiable if the bug is fixed; demonstrates the error handling path
        if (emptyResult != null)
            Assert.False(emptyResult.Success);
    }

    [Fact]
    public void ProcessBatch_SuccessfulOrder_HasPositiveTotal()
    {
        var orders = new List<Order> { MakeOrder(1, 250m), MakeOrder(2, 100m) };
        var results = _processor.ProcessBatch(orders);

        foreach (var r in results.Where(r => r.Success))
            Assert.True(r.FinalTotal > 0);
    }
}
