namespace Blueprint.Modules.Catalog.Contracts;

/// <summary>HTTP-Routen des Catalog-Moduls (gemeinsam genutzt von API, Modul-Client und Blazor-Client).</summary>
public static class CatalogRoutes
{
    public const string Products = "api/catalog/products";

    public const string ProductSnapshots = "api/catalog/products/snapshots";
}
