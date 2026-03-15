using System.Reflection;
using AutoMapper;
using BakeryFlow.Application.Common.Mapping;
using BakeryFlow.Application.Features.Auth;
using BakeryFlow.Application.Features.Categories;
using BakeryFlow.Application.Features.Customers;
using BakeryFlow.Application.Features.Dashboard;
using BakeryFlow.Application.Features.Ingredients;
using BakeryFlow.Application.Features.Inventory;
using BakeryFlow.Application.Features.Products;
using BakeryFlow.Application.Features.Productions;
using BakeryFlow.Application.Features.Purchases;
using BakeryFlow.Application.Features.Recipes;
using BakeryFlow.Application.Features.Reports;
using BakeryFlow.Application.Features.Sales;
using BakeryFlow.Application.Features.Suppliers;
using BakeryFlow.Application.Features.Units;
using BakeryFlow.Application.Features.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BakeryFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
