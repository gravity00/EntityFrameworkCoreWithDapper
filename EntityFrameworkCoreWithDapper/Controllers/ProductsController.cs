using System;
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
                select new ProductModel
                {
                    Id = p.ExternalId,
                    Code = p.Code,
                    Name = p.Name,
                    Price = p
                        .PricesHistory
                        .OrderByDescending(ph => ph.CreatedOn)
                        .First()
                        .Price
                }
            ).OrderBy(p => p.Code).Skip(skip ?? 0).Take(take ?? 20).ToListAsync(ct);
        }

        [HttpPost]
        public async Task<CreateProductResultModel> CreateAsync([FromBody] CreateProductModel model, CancellationToken ct)
        {
            var externalId = Guid.NewGuid();

            var product = new ProductEntity
            {
                ExternalId = externalId,
                Code = model.Code,
                Name = model.Name,
                PricesHistory =
                {
                    new PriceHistoryEntity
                    {
                        Price = model.Price,
                        CreatedOn = DateTime.UtcNow
                    }
                }
            };

            await _context.Set<ProductEntity>().AddAsync(product, ct);

            await _context.SaveChangesAsync(ct);

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
