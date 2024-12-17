using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BidWorker
{
   public class Bid
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public Guid BidId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid AuctionId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public string UserId { get; set; }
        public decimal Value { get; set; }
        public DateTime DateTime { get; set; }
        public string Status { get; set; }

    }
}


 