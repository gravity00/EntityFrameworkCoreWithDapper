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
        public async Task<IEnumerable<ProductModel>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Set<ProductEntity>().OrderBy(e => e.Code).Select(p => new ProductModel
            {
                Id = p.ExternalId,
                Code = p.Code,
                Name = p.Name,
                Price = p.Price
            }).ToListAsync(ct);
        }

        [HttpPost]
        public async Task<CreateProductResultModel> CreateAsync([FromBody] CreateProductModel model, CancellationToken ct)
        {
            var externalId = Guid.NewGuid();

            await _context.Set<ProductEntity>().AddAsync(new ProductEntity
            {
                ExternalId = externalId,
                Code = model.Code,
                Name = model.Name,
                Price = model.Price
            }, ct);

            await _context.SaveChangesAsync(ct);

            return new CreateProductResultModel
            {
                Id = externalId
            };
        }

        [HttpPost("price/multiply")]
        public async Task MultiplyPricesAsync([FromBody] MultiplyProductsPriceModel model, CancellationToken ct)
        {
            var products = await _context.Set<ProductEntity>().ToListAsync(ct);

            foreach (var product in products)
            {
                product.Price *= model.Factor;
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
