using OnlineAuctionWebsite.Models.Entities;
public class DepositVM
{
	public int SessionId { get; set; }
	public string Title { get; set; } = "";
	public decimal Amount { get; set; }
	public PaymentMethod Method { get; set; }
	public IEnumerable<PaymentMethod> Methods { get; set; } = Enumerable.Empty<PaymentMethod>();
}
