using System;
using System.Collections.Generic;

namespace EntityFrameworkCoreWithDapper.Database
{
    public class ProductEntity
    {
        public long Id { get; set; }
        public Guid ExternalId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public virtual ICollection<PriceHistoryEntity> PricesHistory { get; set; }

        public ProductEntity()
        {
            PricesHistory = new HashSet<PriceHistoryEntity>();
        }
    }
}