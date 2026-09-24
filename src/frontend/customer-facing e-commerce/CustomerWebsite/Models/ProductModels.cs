using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Product model for display on the customer site
/// </summary>
public class ProductDisplayModel
{
    public Guid ProductId { get; set; }

    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Short Description")]
    public string ShortDescription { get; set; } = string.Empty;

    [Display(Name = "Price")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Discounted Price")]
    [DataType(DataType.Currency)]
    public decimal? DiscountPrice { get; set; }

    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Brand")]
    public string Brand { get; set; } = string.Empty;

    [Display(Name = "SKU")]
    public string SKU { get; set; } = string.Empty;

    [Display(Name = "Images")]
    public List<ProductImageModel> Images { get; set; } = new();

    [Display(Name = "Availability")]
    public int QuantityAvailable { get; set; }

    [Display(Name = "Rating")]
    public double AverageRating { get; set; }

    [Display(Name = "Review Count")]
    public int ReviewCount { get; set; }

    [Display(Name = "Free Shipping")]
    public bool FreeShipping { get; set; }

    [Display(Name = "Prime Eligible")]
    public bool IsPrimeEligible { get; set; }

    [Display(Name = "Bestseller")]
    public bool IsBestseller { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Last Modified")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Computes the final price factoring in any discount
    /// </summary>
    public decimal FinalPrice => DiscountPrice ?? Price;

    /// <summary>
    /// Computes the discount percentage
    /// </summary>
    public int DiscountPercentage => DiscountPrice.HasValue ?
        (int)Math.Round((1 - (DiscountPrice.Value / Price)) * 100) : 0;

    /// <summary>
    /// Checks whether the product is in stock
    /// </summary>
    public bool IsInStock => QuantityAvailable > 0;
}

/// <summary>
/// Product image model
/// </summary>
public class ProductImageModel
{
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Model for product search
/// </summary>
public class ProductSearchModel
{
    [Display(Name = "Search term")]
    public string? SearchTerm { get; set; }

    [Display(Name = "Category")]
    public string? Category { get; set; }

    [Display(Name = "Brand")]
    public string? Brand { get; set; }

    [Display(Name = "Minimum price")]
    [DataType(DataType.Currency)]
    public decimal? MinPrice { get; set; }

    [Display(Name = "Maximum price")]
    [DataType(DataType.Currency)]
    public decimal? MaxPrice { get; set; }

    [Display(Name = "In-stock products only")]
    public bool OnlyInStock { get; set; } = true;

    [Display(Name = "Prime-eligible products only")]
    public bool OnlyPrimeEligible { get; set; }

    [Display(Name = "Minimum rating")]
    public int? MinRating { get; set; }

    [Display(Name = "Sort by")]
    public ProductSortOrder SortOrder { get; set; } = ProductSortOrder.Relevance;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Enumeration for product sort order
/// </summary>
public enum ProductSortOrder
{
    [Display(Name = "Relevance")]
    Relevance,

    [Display(Name = "Price: low to high")]
    PriceAscending,

    [Display(Name = "Price: high to low")]
    PriceDescending,

    [Display(Name = "Customer rating")]
    CustomerRating,

    [Display(Name = "Newest first")]
    NewestFirst,

    [Display(Name = "Bestseller")]
    Bestseller
}

/// <summary>
/// Search results model
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
/// Product category model
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
