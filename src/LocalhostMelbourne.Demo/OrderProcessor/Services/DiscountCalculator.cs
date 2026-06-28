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

    /// <summary>
    /// Calculates a bundle discount for orders with more than 3 items.
    /// </summary>
    /// <param name="quantity"></param>
    /// <param name="unitPrice"></param>
    /// <returns></returns>
    public decimal CalculateBundleDiscount(int quantity, decimal unitPrice)
    {
        if (quantity > 3)   // BUG: should be >= 3
            return quantity * unitPrice * 0.10m;

        return 0m;
    }

    /// <summary>
    /// Calculates the total discount for an order based on loyalty tier and promo code.
    /// </summary>
    /// <param name="subtotal"></param>
    /// <param name="tier"></param>
    /// <param name="promoCode"></param>
    /// <returns></returns>
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


    /// <summary>
    /// Calculates the tax for an order based on subtotal, discount amount, and tax rate.
    /// </summary>
    /// <param name="subtotal"></param>
    /// <param name="discountAmount"></param>
    /// <param name="taxRate"></param>
    /// <returns></returns>
    public decimal CalculateTax(decimal subtotal, decimal discountAmount, decimal taxRate = 0.10m)
    {
        return (subtotal - discountAmount) * taxRate;   // BUG: should be subtotal * taxRate
    }
}
