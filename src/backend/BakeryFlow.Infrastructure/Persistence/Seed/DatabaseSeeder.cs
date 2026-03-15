using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Domain.Common;
using BakeryFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BakeryFlow.Infrastructure.Persistence.Seed;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseSeeder(
    BakeryFlowDbContext dbContext,
    IConfiguration configuration,
    IPasswordHasher passwordHasher,
    ILogger<DatabaseSeeder> logger) : IDatabaseSeeder
{
    private const string DefaultAdminEmail = "admin@bakeryflow.local";
    private const string DefaultAdminName = "Administrador";
    private const string DefaultAdminPassword = "Bakey2026*";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        await SeedAdminAsync(cancellationToken);
        await SeedUnitsAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var email = (configuration["ADMIN_EMAIL"] ?? DefaultAdminEmail).Trim().ToLower();
        var password = configuration["ADMIN_PASSWORD"] ?? DefaultAdminPassword;
        var adminName = string.IsNullOrWhiteSpace(configuration["ADMIN_NAME"])
            ? DefaultAdminName
            : configuration["ADMIN_NAME"]!.Trim();

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (existingUser is not null)
        {
            logger.LogInformation("El usuario administrador inicial {Email} ya existe. No se modificará.", email);
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No se pudo crear el usuario administrador inicial porque faltan ADMIN_EMAIL o ADMIN_PASSWORD.");
            return;
        }

        var (firstName, lastName) = SplitName(adminName);

        dbContext.Users.Add(new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = SystemRoles.Admin,
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Usuario administrador inicial creado: {Email}", email);
    }

    private async Task SeedUnitsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.UnitsOfMeasure.AnyAsync(cancellationToken))
        {
            return;
        }

        var units = new[]
        {
            new UnitOfMeasure { Name = "Gramo", Abbreviation = "g", Type = "Peso" },
            new UnitOfMeasure { Name = "Kilogramo", Abbreviation = "kg", Type = "Peso" },
            new UnitOfMeasure { Name = "Mililitro", Abbreviation = "ml", Type = "Volumen" },
            new UnitOfMeasure { Name = "Litro", Abbreviation = "l", Type = "Volumen" },
            new UnitOfMeasure { Name = "Unidad", Abbreviation = "und", Type = "Unidad" },
            new UnitOfMeasure { Name = "Caja", Abbreviation = "caja", Type = "Empaque" }
        };

        dbContext.UnitsOfMeasure.AddRange(units);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return (DefaultAdminName, string.Empty);
        }

        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
