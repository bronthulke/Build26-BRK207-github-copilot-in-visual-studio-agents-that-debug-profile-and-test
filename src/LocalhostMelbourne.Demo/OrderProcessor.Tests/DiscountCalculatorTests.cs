using OrderProcessor.Models;
using OrderProcessor.Services;
using Xunit;

namespace OrderProcessor.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

    // -----------------------------------------------------------------------
    // CalculateBundleDiscount — spec: qty >= 3 qualifies for 10% bundle discount
    // -----------------------------------------------------------------------

    [Fact]
    public void BundleDiscount_ZeroForQuantityOne()
    {
        decimal result = _calc.CalculateBundleDiscount(1, 100m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void BundleDiscount_ZeroForQuantityTwo()
    {
        decimal result = _calc.CalculateBundleDiscount(2, 100m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void BundleDiscount_TenPercentForExactlyThreeUnits()
    {
        decimal result = _calc.CalculateBundleDiscount(3, 100m);
        Assert.Equal(30m, result);   // 3 × $100 × 10%
    }

    [Fact]
    public void BundleDiscount_TenPercentForMoreThanThreeUnits()
    {
        decimal result = _calc.CalculateBundleDiscount(5, 50m);
        Assert.Equal(25m, result);   // 5 × $50 × 10%
    }

    [Fact]  // FAILS: same off-by-one; qty=3 at real price fails
    public void BundleDiscount_ThreeKeyboardsGetTenPercent()
    {
        // "Buy 3 Mechanical Keyboards @ $149.99, expect 10% bundle discount"
        decimal result = _calc.CalculateBundleDiscount(3, 149.99m);
        Assert.Equal(44.997m, result);
    }

    // -----------------------------------------------------------------------
    // CalculateTotalDiscount — spec: both discounts applied to original subtotal independently
    // -----------------------------------------------------------------------

    [Fact]
    public void TotalDiscount_BronzeWithNoPromoIsZero()
    {
        var discount = _calc.CalculateTotalDiscount(500m, LoyaltyTier.Bronze, null);
        Assert.Equal(0m, discount.TotalDiscount);
    }

    [Fact]
    public void TotalDiscount_SilverLoyaltyOnly()
    {
        // Silver = 5%; no promo
        var discount = _calc.CalculateTotalDiscount(200m, LoyaltyTier.Silver, null);
        Assert.Equal(10m, discount.LoyaltyDiscount);    // 200 × 5%
        Assert.Equal(0m, discount.PromoDiscount);
        Assert.Equal(10m, discount.TotalDiscount);
    }

    [Fact]
    public void TotalDiscount_GoldLoyaltyPlusSave10Promo()
    {
        // Gold = 10%; SAVE10 = 10%; both applied to $200 original subtotal independently
        // Expected: loyalty=$20, promo=$20, total=$40
        var discount = _calc.CalculateTotalDiscount(200m, LoyaltyTier.Gold, "SAVE10");
        Assert.Equal(20m, discount.LoyaltyDiscount);
        Assert.Equal(20m, discount.PromoDiscount);
        Assert.Equal(40m, discount.TotalDiscount);
    }

    [Fact]
    public void TotalDiscount_PlatinumPlusSave20Promo()
    {
        // Platinum = 15%; SAVE20 = 20%; on $1000 subtotal
        // Expected: loyalty=$150, promo=$200, total=$350
        var discount = _calc.CalculateTotalDiscount(1000m, LoyaltyTier.Platinum, "SAVE20");
        Assert.Equal(150m, discount.LoyaltyDiscount);
        Assert.Equal(200m, discount.PromoDiscount);
        Assert.Equal(350m, discount.TotalDiscount);
    }

    [Fact]
    public void TotalDiscount_UnknownPromoCodeIgnored()
    {
        var discount = _calc.CalculateTotalDiscount(100m, LoyaltyTier.Silver, "BOGUS99");
        Assert.Equal(5m, discount.LoyaltyDiscount);
        Assert.Equal(0m, discount.PromoDiscount);
    }

    // -----------------------------------------------------------------------
    // CalculateTax — spec: GST is 10% of original subtotal (before any discounts)
    // -----------------------------------------------------------------------

    [Fact]  // FAILS: tax is calculated on (subtotal - discount) instead of subtotal
    public void Tax_CalculatedOnSubtotalNotDiscountedAmount()
    {
        // $200 subtotal, $40 discount → tax should be 10% of $200 = $20
        decimal tax = _calc.CalculateTax(200m, 40m);
        Assert.Equal(20m, tax);
    }

    [Fact]  // FAILS: same issue
    public void Tax_NoDiscountStillCorrect()
    {
        decimal tax = _calc.CalculateTax(500m, 0m);
        Assert.Equal(50m, tax);
    }

    [Fact]
    public void Tax_LargeDiscountDoesNotReduceTaxBase()
    {
        // $1000 subtotal, $300 discount → tax must still be $100 (10% of subtotal)
        decimal tax = _calc.CalculateTax(1000m, 300m);
        Assert.Equal(100m, tax);
    }
}
