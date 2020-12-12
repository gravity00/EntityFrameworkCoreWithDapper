﻿using System;

namespace EntityFrameworkCoreWithDapper.Database
{
    public class ProductEntity
    {
        public long Id { get; set; }
        public Guid ExternalId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}