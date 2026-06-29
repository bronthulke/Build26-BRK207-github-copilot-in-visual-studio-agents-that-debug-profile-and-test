using OrderProcessor.Models;

namespace OrderProcessor.Services;

/// <summary>
/// Processes orders in configurable batches. Used by the nightly fulfilment pipeline
/// to work through large queues without overwhelming downstream inventory and payment services.
/// </summary>
public class OrderBatchProcessor
{
    private readonly DiscountCalculator _discountCalculator;

    public OrderBatchProcessor(DiscountCalculator discountCalculator)
    {
        _discountCalculator = discountCalculator;
    }

    // SCENARIO 1 (DEBUGGING) — BUG:
    // The loop condition is `orders.Count - 1` instead of `orders.Count`.
    // Every batch silently drops its last order. With a batch of 1 order, nothing is processed.
    // The symptom: total orders processed never equals total orders submitted; the gap grows
    // with the number of batches. Difficult to spot because the logic inside the loop is correct.
    public IReadOnlyList<OrderResult> ProcessBatch(IList<Order> orders)
    {
        var results = new List<OrderResult>();

        for (int i = 0; i < orders.Count - 1; i++)
        {
            var result = ProcessSingleOrder(orders[i]);
            results.Add(result);
        }

        return results;
    }

    private OrderResult ProcessSingleOrder(Order order)
    {
        try
        {
            if (order.Items.Count == 0)
                return Fail(order.Id, "Order has no items.");

            decimal subtotal = order.Items.Sum(i => i.LineTotal);
            var discount = _discountCalculator.CalculateTotalDiscount(subtotal, LoyaltyTier.Silver, order.PromoCode);
            decimal tax = _discountCalculator.CalculateTax(subtotal, discount.TotalDiscount);
            decimal total = subtotal - discount.TotalDiscount + tax;

            order.SubTotal = subtotal;
            order.DiscountAmount = discount.TotalDiscount;
            order.TaxAmount = tax;
            order.Total = total;
            order.Status = OrderStatus.Processing;

            return new OrderResult
            {
                OrderId = order.Id,
                Success = true,
                FinalTotal = total
            };
        }
        catch (Exception ex)
        {
            return Fail(order.Id, ex.Message);
        }
    }

    private static OrderResult Fail(string orderId, string message) =>
        new() { OrderId = orderId, Success = false, ErrorMessage = message };
}
