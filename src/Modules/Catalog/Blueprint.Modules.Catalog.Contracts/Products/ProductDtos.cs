using System.ComponentModel.DataAnnotations;

namespace Blueprint.Modules.Catalog.Contracts.Products;

/// <summary>Request zum Anlegen eines Produkts.</summary>
/// <remarks>
/// Validierung per DataAnnotations – serverseitig durch die eingebaute Minimal-API-Validierung (.NET 10),
/// clientseitig durch Blazor <c>DataAnnotationsValidator</c>:
/// https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis#validation-support-in-minimal-apis<br/>
/// https://learn.microsoft.com/aspnet/core/blazor/forms/validation
/// </remarks>
public sealed class CreateProductRequest
{
    [Required, StringLength(32, MinimumLength = 3)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EUR";

    [Range(0, 1_000_000)]
    public int InitialStock { get; set; }
}

public sealed record ProductResponse(Guid Id, string Sku, string Name, string? Description, decimal Price, string Currency, int StockQuantity);
