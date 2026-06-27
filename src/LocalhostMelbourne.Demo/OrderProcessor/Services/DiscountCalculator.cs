using OrderProcessor.Models;

namespace OrderProcessor.Services;

/// <summary>
/// Calculates discounts for orders based on loyalty tier, bundle rules, and promo codes.
/// </summary>
public class DiscountCalculator
{
    private static readonly Dictionary<string, decimal> _promoCodes = new()
    {
        ["SAVE10"] = 0.10m,
        ["SAVE20"] = 0.20m,
        ["WELCOME"] = 0.15m
    };

    public static readonly Dictionary<LoyaltyTier, decimal> LoyaltyRates = new()
    {
        [LoyaltyTier.Bronze]   = 0.00m,
        [LoyaltyTier.Silver]   = 0.05m,
        [LoyaltyTier.Gold]     = 0.10m,
        [LoyaltyTier.Platinum] = 0.15m
    };

    // SCENARIO 3 (TESTING) — BUG 1:
    // Bundle discount spec: "Buy 3 or more units of the same item, get 10% off that line."
    // Bug: condition uses > 3 instead of >= 3, so an order of exactly 3 units gets no discount.
    public decimal CalculateBundleDiscount(int quantity, decimal unitPrice)
    {
        if (quantity > 3)   // BUG: should be >= 3
            return quantity * unitPrice * 0.10m;

        return 0m;
    }

    // SCENARIO 3 (TESTING) — BUG 2:
    // Spec: both loyalty and promo discounts are calculated independently against the original subtotal,
    // then summed. e.g. subtotal=$200, loyalty=10%, promo=10% → discount = $20 + $20 = $40.
    // Bug: promo code is applied to the post-loyalty-discount remainder, which under-discounts.
    public OrderDiscount CalculateTotalDiscount(decimal subtotal, LoyaltyTier tier, string? promoCode)
    {
        decimal loyaltyRate = LoyaltyRates[tier];
        decimal loyaltyDiscount = subtotal * loyaltyRate;

        decimal remainingAfterLoyalty = subtotal - loyaltyDiscount;   // BUG: promo should use subtotal, not remainder

        decimal promoRate = promoCode != null && _promoCodes.TryGetValue(promoCode, out var rate) ? rate : 0m;
        decimal promoDiscount = remainingAfterLoyalty * promoRate;   // BUG: should be subtotal * promoRate

        return new OrderDiscount
        {
            LoyaltyDiscount = loyaltyDiscount,
            PromoDiscount = promoDiscount
        };
    }

    // SCENARIO 3 (TESTING) — BUG 3:
    // Spec: tax (GST) is calculated on the original subtotal before discounts.
    // Bug: tax is calculated on (subtotal - discount), which under-charges tax.
    public decimal CalculateTax(decimal subtotal, decimal discountAmount, decimal taxRate = 0.10m)
    {
        return (subtotal - discountAmount) * taxRate;   // BUG: should be subtotal * taxRate
    }
}
