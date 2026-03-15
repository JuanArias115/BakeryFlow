using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Domain.Common;
using BakeryFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BakeryFlow.Infrastructure.Persistence;

public sealed class BakeryFlowDbContext(DbContextOptions<BakeryFlowDbContext> options)
    : DbContext(options), IBakeryFlowDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeDetail> RecipeDetails => Set<RecipeDetail>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseDetail> PurchaseDetails => Set<PurchaseDetail>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Production> Productions => Set<Production>();
    public DbSet<ProductionDetail> ProductionDetails => Set<ProductionDetail>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureUnit(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureIngredient(modelBuilder);
        ConfigureSupplier(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureRecipe(modelBuilder);
        ConfigurePurchase(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureProduction(modelBuilder);
        ConfigureSale(modelBuilder);
        ApplyUtcDateTimeConverters(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<AuditableEntity>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(160).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
        });
    }

    private static void ConfigureUnit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Abbreviation).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Abbreviation).IsUnique();
        });
    }

    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.UnitSale).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.SalePrice).HasPrecision(18, 2);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureIngredient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.StockCurrent).HasPrecision(18, 4);
            entity.Property(x => x.StockMinimum).HasPrecision(18, 4);
            entity.Property(x => x.AverageCost).HasPrecision(18, 4);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasOne(x => x.UnitOfMeasure)
                .WithMany(x => x.Ingredients)
                .HasForeignKey(x => x.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSupplier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(160);
            entity.Property(x => x.Address).HasMaxLength(250);
            entity.Property(x => x.Contact).HasMaxLength(120);
            entity.Property(x => x.Notes).HasMaxLength(500);
        });
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(160);
            entity.Property(x => x.Address).HasMaxLength(250);
            entity.Property(x => x.Notes).HasMaxLength(500);
        });
    }

    private static void ConfigureRecipe(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(x => x.Yield).HasPrecision(18, 4);
            entity.Property(x => x.YieldUnit).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PackagingCost).HasPrecision(18, 4);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasOne(x => x.Product)
                .WithMany(x => x.Recipes)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecipeDetail>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.CalculatedUnitCost).HasPrecision(18, 4);
            entity.Property(x => x.CalculatedTotalCost).HasPrecision(18, 4);
            entity.HasOne(x => x.Recipe)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Ingredient)
                .WithMany(x => x.RecipeDetails)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UnitOfMeasure)
                .WithMany()
                .HasForeignKey(x => x.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePurchase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.Property(x => x.InvoiceNumber).HasMaxLength(80);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.HasOne(x => x.Supplier)
                .WithMany(x => x.Purchases)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseDetail>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.HasOne(x => x.Purchase)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Ingredient)
                .WithMany(x => x.PurchaseDetails)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UnitOfMeasure)
                .WithMany()
                .HasForeignKey(x => x.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.Property(x => x.QuantityIn).HasPrecision(18, 4);
            entity.Property(x => x.QuantityOut).HasPrecision(18, 4);
            entity.Property(x => x.ResultingBalance).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.Notes).HasMaxLength(300);
            entity.HasOne(x => x.Ingredient)
                .WithMany(x => x.InventoryMovements)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProduction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Production>(entity =>
        {
            entity.Property(x => x.QuantityToProduce).HasPrecision(18, 4);
            entity.Property(x => x.QuantityActual).HasPrecision(18, 4);
            entity.Property(x => x.TotalCost).HasPrecision(18, 4);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasOne(x => x.Recipe)
                .WithMany(x => x.Productions)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductionDetail>(entity =>
        {
            entity.Property(x => x.QuantityConsumed).HasPrecision(18, 4);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.TotalCost).HasPrecision(18, 4);
            entity.HasOne(x => x.Production)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.ProductionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Ingredient)
                .WithMany(x => x.ProductionDetails)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSale(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.UnitCost).HasPrecision(18, 4);
            entity.Property(x => x.Profit).HasPrecision(18, 2);
            entity.HasOne(x => x.Sale)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(x => x.SaleDetails)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
            value => NormalizeDateTime(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var nullableUtcDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            value => value.HasValue ? NormalizeDateTime(value.Value) : value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcDateTimeConverter);
                }
            }
        }
    }

    private static DateTime NormalizeDateTime(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
}
