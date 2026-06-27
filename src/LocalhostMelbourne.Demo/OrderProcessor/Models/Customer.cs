namespace OrderProcessor.Models;

public enum LoyaltyTier
{
    Bronze,
    Silver,
    Gold,
    Platinum
}

public class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public LoyaltyTier Tier { get; set; }
    public decimal TotalHistoricSpend { get; set; }
    public DateTime MemberSince { get; set; }
}
