namespace BidWorker
{
   public class Bid
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AuctionId { get; set; }
        public string BidderName { get; set; }
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}


 