namespace BidWorker
{
    public class Bid
    {
        public int Id { get; set; }
        public int AuctionId { get; set; }
        public string BidderName { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}


 