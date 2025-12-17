using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Modello del prodotto per la visualizzazione nel sito del cliente
/// </summary>
public class ProductDisplayModel
{
    public Guid ProductId { get; set; }
    
    [Display(Name = "Nome Prodotto")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Descrizione")]
    public string Description { get; set; } = string.Empty;
    
    [Display(Name = "Descrizione Breve")]
    public string ShortDescription { get; set; } = string.Empty;
    
    [Display(Name = "Prezzo")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }
    
    [Display(Name = "Prezzo Scontato")]
    [DataType(DataType.Currency)]
    public decimal? DiscountPrice { get; set; }
    
    [Display(Name = "Categoria")]
    public string Category { get; set; } = string.Empty;
    
    [Display(Name = "Brand")]
    public string Brand { get; set; } = string.Empty;
    
    [Display(Name = "SKU")]
    public string SKU { get; set; } = string.Empty;
    
    [Display(Name = "Immagini")]
    public List<ProductImageModel> Images { get; set; } = new();
    
    [Display(Name = "Disponibilità")]
    public int QuantityAvailable { get; set; }
    
    [Display(Name = "Rating")]
    public double AverageRating { get; set; }
    
    [Display(Name = "Numero Recensioni")]
    public int ReviewCount { get; set; }
    
    [Display(Name = "Spedizione Gratuita")]
    public bool FreeShipping { get; set; }
    
    [Display(Name = "Prime Eligible")]
    public bool IsPrimeEligible { get; set; }
    
    [Display(Name = "Bestseller")]
    public bool IsBestseller { get; set; }
    
    [Display(Name = "Data Creazione")]
    public DateTime CreatedAt { get; set; }
    
    [Display(Name = "Ultima Modifica")]
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Calcola il prezzo finale considerando eventuali sconti
    /// </summary>
    public decimal FinalPrice => DiscountPrice ?? Price;
    
    /// <summary>
    /// Calcola la percentuale di sconto
    /// </summary>
    public int DiscountPercentage => DiscountPrice.HasValue ? 
        (int)Math.Round((1 - (DiscountPrice.Value / Price)) * 100) : 0;
    
    /// <summary>
    /// Verifica se il prodotto è disponibile
    /// </summary>
    public bool IsInStock => QuantityAvailable > 0;
}

/// <summary>
/// Modello delle immagini del prodotto
/// </summary>
public class ProductImageModel
{
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Modello per la ricerca dei prodotti
/// </summary>
public class ProductSearchModel
{
    [Display(Name = "Termine di ricerca")]
    public string? SearchTerm { get; set; }
    
    [Display(Name = "Categoria")]
    public string? Category { get; set; }
    
    [Display(Name = "Brand")]
    public string? Brand { get; set; }
    
    [Display(Name = "Prezzo minimo")]
    [DataType(DataType.Currency)]
    public decimal? MinPrice { get; set; }
    
    [Display(Name = "Prezzo massimo")]
    [DataType(DataType.Currency)]
    public decimal? MaxPrice { get; set; }
    
    [Display(Name = "Solo prodotti disponibili")]
    public bool OnlyInStock { get; set; } = true;
    
    [Display(Name = "Solo prodotti Prime")]
    public bool OnlyPrimeEligible { get; set; }
    
    [Display(Name = "Rating minimo")]
    public int? MinRating { get; set; }
    
    [Display(Name = "Ordinamento")]
    public ProductSortOrder SortOrder { get; set; } = ProductSortOrder.Relevance;
    
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Enumerazione per l'ordinamento dei prodotti
/// </summary>
public enum ProductSortOrder
{
    [Display(Name = "Rilevanza")]
    Relevance,
    
    [Display(Name = "Prezzo: dal più basso")]
    PriceAscending,
    
    [Display(Name = "Prezzo: dal più alto")]
    PriceDescending,
    
    [Display(Name = "Rating del cliente")]
    CustomerRating,
    
    [Display(Name = "Data di pubblicazione")]
    NewestFirst,
    
    [Display(Name = "Bestseller")]
    Bestseller
}

/// <summary>
/// Modello dei risultati della ricerca
/// </summary>
public class ProductSearchResultModel
{
    public List<ProductDisplayModel> Products { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public ProductSearchModel SearchCriteria { get; set; } = new();
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

/// <summary>
/// Modello delle categorie di prodotti
/// </summary>
public class CategoryModel
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public List<CategoryModel> SubCategories { get; set; } = new();
    public string? ParentCategory { get; set; }
}