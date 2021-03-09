using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCoreWithDapper.Database;
using Microsoft.AspNetCore.Mvc;

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
            return await _context.QueryAsync<ProductModel>(ct, @"
SELECT p.ExternalId as Id, p.Code, p.Name, lph.Price, lph.CreatedOn as PriceChangedOn
FROM (
    SELECT Id, ExternalId, Code, Name, RowId
    FROM Product
    ORDER BY Code DESC
    LIMIT @Take OFFSET @Skip
) p
INNER JOIN (
    SELECT ph.ProductId, ph.Price, ph.CreatedOn
    FROM PriceHistory ph
    INNER JOIN (
        SELECT MAX(RowId) RowId
        FROM PriceHistory
        GROUP BY ProductId
    ) phLatest ON ph.RowId = phLatest.RowId
) lph ON p.Id = lph.ProductId", new
            {
                Skip = skip ?? 0,
                Take = take ?? 20
            });
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
    }
}
