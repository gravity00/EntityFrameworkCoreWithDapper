using System;

namespace EntityFrameworkCoreWithDapper.Database
{
    public class PriceHistoryEntity
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}