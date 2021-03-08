﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCoreWithDapper.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCoreWithDapper.Controllers
{
    [Route("products")]
    public class ProductsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public ProductsController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IEnumerable<ProductModel>> GetAllAsync([FromQuery] int? skip, [FromQuery] int? take, CancellationToken ct)
        {
            return await (
                    from p in _context.Set<ProductEntity>()
                    select new
                    {
                        Id = p.ExternalId,
                        p.Code,
                        p.Name,
                        MostRecentPriceHistory = p
                            .PricesHistory
                            .OrderByDescending(ph => ph.CreatedOn)
                            .First()
                    }
                )
                .OrderBy(p => p.Code)
                .Skip(skip ?? 0)
                .Take(take ?? 20)
                .Select(p => new ProductModel
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Price = p.MostRecentPriceHistory.Price,
                    PriceChangedOn = p.MostRecentPriceHistory.CreatedOn
                })
                .ToListAsync(ct);
        }

        [HttpPost]
        public async Task<CreateProductResultModel> CreateAsync([FromBody] CreateProductModel model, CancellationToken ct)
        {
            var externalId = Guid.NewGuid();

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            await _context.ExecuteAsync(ct, @"
INSERT INTO Product (ExternalId, Code, Name)
VALUES (@ExternalId, @Code, @Name);

INSERT INTO PriceHistory (Price, CreatedOn, ProductId)
SELECT @Price, @CreatedOn, Id
FROM Product
WHERE
    rowid = last_insert_rowid();", new
            {
                ExternalId = externalId,
                model.Code,
                model.Name,
                model.Price,
                CreatedOn = DateTime.UtcNow
            });

            await tx.CommitAsync(ct);

            return new CreateProductResultModel
            {
                Id = externalId
            };
        }

        [HttpPost("price/multiply")]
        public async Task MultiplyPricesAsync([FromBody] MultiplyProductsPriceModel model, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            const int batchSize = 10;
            var skip = 0;
            List<PriceHistoryEntity> pricesHistory;
            do
            {
                pricesHistory = await _context
                    .Set<ProductEntity>()
                    .OrderBy(p => p.Id)
                    .Skip(skip)
                    .Take(batchSize)
                    .Select(p => p.PricesHistory
                        .AsQueryable()
                        .Include(ph => ph.Product)
                        .OrderByDescending(ph => ph.CreatedOn)
                        .First())
                    .ToListAsync(ct);

                if (pricesHistory.Count > 0)
                {
                    await _context
                        .Set<PriceHistoryEntity>()
                        .AddRangeAsync(pricesHistory.Select(ph => new PriceHistoryEntity
                        {
                            Product = ph.Product,
                            Price = ph.Price * model.Factor,
                            CreatedOn = now
                        }), ct);

                    await _context.SaveChangesAsync(ct);
                }

                skip += pricesHistory.Count;
            } while (pricesHistory.Count > 0);

            await tx.CommitAsync(ct);
        }
    }
}
