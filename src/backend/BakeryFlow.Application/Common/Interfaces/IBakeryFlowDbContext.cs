using BakeryFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Common.Interfaces;

public interface IBakeryFlowDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<Ingredient> Ingredients { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeDetail> RecipeDetails { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseDetail> PurchaseDetails { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<Production> Productions { get; }
    DbSet<ProductionDetail> ProductionDetails { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleDetail> SaleDetails { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
