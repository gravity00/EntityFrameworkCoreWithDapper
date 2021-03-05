using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCoreWithDapper.Database
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProductEntity>(cfg =>
            {
                cfg.ToTable("Product");

                cfg.HasKey(e => e.Id);
                cfg.HasAlternateKey(e => e.ExternalId);

                cfg.HasIndex(e => e.Code).IsUnique();

                cfg.Property(e => e.Id)
                    .IsRequired()
                    .ValueGeneratedOnAdd();
                cfg.Property(e => e.ExternalId)
                    .IsRequired();
                cfg.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(8);
                cfg.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(128);
            });

            builder.Entity<PriceHistoryEntity>(cfg =>
            {
                cfg.ToTable("PriceHistory");

                cfg.HasKey(e => e.Id);

                cfg.HasIndex(e => e.CreatedOn);

                cfg.Property(e => e.Id)
                    .IsRequired()
                    .ValueGeneratedOnAdd();
                cfg.HasOne(e => e.Product)
                    .WithMany(p => p.PricesHistory)
                    .IsRequired();
                cfg.Property(e => e.Price)
                    .IsRequired();
                cfg.Property(e => e.CreatedOn)
                    .IsRequired();
            });
        }
    }
}
