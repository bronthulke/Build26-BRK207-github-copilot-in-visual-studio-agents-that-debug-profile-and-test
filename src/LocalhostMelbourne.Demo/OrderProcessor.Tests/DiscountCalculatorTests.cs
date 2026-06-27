using OrderProcessor.Models;
using OrderProcessor.Services;
using Xunit;

namespace OrderProcessor.Tests;

/// <summary>
/// SCENARIO 3 (TESTING): These tests define the correct behaviour of DiscountCalculator.
/// Several tests are EXPECTED TO FAIL because the implementation contains deliberate bugs.
/// Use GitHub Copilot to investigate the failures and identify the root cause.
///
/// Copilot prompt: "Some of these tests are failing. Investigate the failures and explain
/// what is wrong in DiscountCalculator, then suggest the minimal code change to fix each bug."
/// </summary>
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

    [Fact]  // FAILS: bug uses > 3 instead of >= 3; qty=3 returns 0 instead of $30
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

    [Fact]  // FAILS: promo is applied to (subtotal - loyaltyDiscount) instead of subtotal
    public void TotalDiscount_GoldLoyaltyPlusSave10Promo()
    {
        // Gold = 10%; SAVE10 = 10%; both applied to $200 original subtotal independently
        // Expected: loyalty=$20, promo=$20, total=$40
        var discount = _calc.CalculateTotalDiscount(200m, LoyaltyTier.Gold, "SAVE10");
        Assert.Equal(20m, discount.LoyaltyDiscount);
        Assert.Equal(20m, discount.PromoDiscount);      // BUG returns $18 (10% of $180)
        Assert.Equal(40m, discount.TotalDiscount);
    }

    [Fact]  // FAILS: same stacking bug with SAVE20
    public void TotalDiscount_PlatinumPlusSave20Promo()
    {
        // Platinum = 15%; SAVE20 = 20%; on $1000 subtotal
        // Expected: loyalty=$150, promo=$200, total=$350
        var discount = _calc.CalculateTotalDiscount(1000m, LoyaltyTier.Platinum, "SAVE20");
        Assert.Equal(150m, discount.LoyaltyDiscount);
        Assert.Equal(200m, discount.PromoDiscount);     // BUG returns $170 (20% of $850)
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
        Assert.Equal(20m, tax);   // BUG returns $16 (10% of $160)
    }

    [Fact]  // FAILS: same issue
    public void Tax_NoDiscountStillCorrect()
    {
        decimal tax = _calc.CalculateTax(500m, 0m);
        Assert.Equal(50m, tax);   // 10% of $500; passes (discount=0 hides the bug)
    }

    [Fact]  // FAILS: the bug only shows when discount > 0
    public void Tax_LargeDiscountDoesNotReduceTaxBase()
    {
        // $1000 subtotal, $300 discount → tax must still be $100 (10% of subtotal)
        decimal tax = _calc.CalculateTax(1000m, 300m);
        Assert.Equal(100m, tax);  // BUG returns $70 (10% of $700)
    }
}
