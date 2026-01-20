using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all products
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero di tutti i prodotti");
            var products = await _productService.GetAllProductsAsync(ct);
            
            _logger.LogInformation("Recuperati con successo {ProductCount} prodotti", 
                products?.Count() ?? 0);
            
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero di tutti i prodotti");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Product details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Product>> GetProduct(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero prodotto con ID: {ProductId}", id);
            var product = await _productService.GetProductByIdAsync(id, ct);

            if (product == null)
            {
                _logger.LogInformation("Prodotto con ID {ProductId} non trovato", id);
                return NotFound($"Product with ID {id} not found");
            }

            _logger.LogInformation("Prodotto recuperato con successo: {ProductId}", id);
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del prodotto {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="product">Product to create</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest product, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Modello prodotto non valido fornito");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creazione nuovo prodotto: {ProductName}", product.Name);

            var newProduct = new Product
            {
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                IsActive = true
            };

            await _productService.AddProductAsync(newProduct, ct);

            _logger.LogInformation("Prodotto creato con successo con ID: {ProductId}", newProduct.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione del prodotto");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="product">Updated product data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Product>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest product, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Modello prodotto non valido fornito per ID: {ProductId}", id);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Aggiornamento prodotto con ID: {ProductId}", id);

            // Verifica se il prodotto esistente
            var existingProduct = await _productService.GetProductByIdAsync(id, ct);
            if (existingProduct == null)
            {
                _logger.LogInformation("Prodotto con ID {ProductId} non trovato per aggiornamento", id);
                return NotFound($"Product with ID {id} not found");
            }

            // Aggiorna le proprietà del prodotto
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            existingProduct.IsActive = product.IsActive;

            await _productService.UpdateProductAsync(existingProduct, ct);

            _logger.LogInformation("Prodotto aggiornato con successo con ID: {ProductId}", id);
            return Ok(existingProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del prodotto {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Eliminazione prodotto con ID: {ProductId}", id);

            var deleted = await _productService.DeleteProductAsync(id, ct);
            if (!deleted)
            {
                _logger.LogInformation("Prodotto con ID {ProductId} non trovato per eliminazione", id);
                return NotFound($"Product with ID {id} not found");
            }

            _logger.LogInformation("Prodotto eliminato con successo con ID: {ProductId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione del prodotto {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Search products with filters
    /// </summary>
    /// <param name="searchTerm">Search term for product name or description</param>
    /// <param name="category">Filter by category</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching products</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Product>>> SearchProducts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? isActive = true,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Ricerca prodotti con filtri: SearchTerm={SearchTerm}, Category={Category}", searchTerm, category);
            
            var products = await _productService.GetAllProductsAsync(ct);
            
            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                products = products.Where(p => 
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrWhiteSpace(category))
            {
                // For now, we'll add category support later
                _logger.LogWarning("Category filtering not yet implemented");
            }
            
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }
            
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }
            
            if (isActive.HasValue)
            {
                products = products.Where(p => p.IsActive == isActive.Value);
            }
            
            var result = products.ToList();
            _logger.LogInformation("Trovati {Count} prodotti corrispondenti", result.Count);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricerca prodotti");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get featured products
    /// </summary>
    /// <param name="count">Number of featured products to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of featured products</returns>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Product>>> GetFeaturedProducts(
        [FromQuery] int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero {Count} prodotti in evidenza", count);
            
            var products = await _productService.GetAllProductsAsync(ct);
            var featured = products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Price) // Simple logic: highest priced items
                .Take(count)
                .ToList();
            
            return Ok(featured);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti in evidenza");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get bestseller products
    /// </summary>
    /// <param name="count">Number of bestseller products to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of bestseller products</returns>
    [HttpGet("bestsellers")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Product>>> GetBestsellers(
        [FromQuery] int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero {Count} prodotti più venduti", count);
            
            var products = await _productService.GetAllProductsAsync(ct);
            // TODO: Implement actual sales tracking
            // For now, return random active products
            var bestsellers = products
                .Where(p => p.IsActive)
                .Take(count)
                .ToList();
            
            return Ok(bestsellers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei bestseller");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get recommended products
    /// </summary>
    /// <param name="count">Number of recommended products to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of recommended products</returns>
    [HttpGet("recommended")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Product>>> GetRecommendedProducts(
        [FromQuery] int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero {Count} prodotti consigliati", count);
            
            var products = await _productService.GetAllProductsAsync(ct);
            // TODO: Implement recommendation engine
            // For now, return active products
            var recommended = products
                .Where(p => p.IsActive)
                .Take(count)
                .ToList();
            
            return Ok(recommended);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti consigliati");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get related products for a specific product
    /// </summary>
    /// <param name="productId">Product ID to find related products for</param>
    /// <param name="count">Number of related products to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of related products</returns>
    [HttpGet("{productId:guid}/related")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Product>>> GetRelatedProducts(
        Guid productId,
        [FromQuery] int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero prodotti correlati per {ProductId}", productId);
            
            var product = await _productService.GetProductByIdAsync(productId, ct);
            if (product == null)
            {
                return NotFound($"Product with ID {productId} not found");
            }
            
            var allProducts = await _productService.GetAllProductsAsync(ct);
            // TODO: Implement proper related products logic based on category, tags, etc.
            // For now, return other active products excluding the current one
            var related = allProducts
                .Where(p => p.IsActive && p.Id != productId)
                .Take(count)
                .ToList();
            
            return Ok(related);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti correlati");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of categories</returns>
    [HttpGet("~/api/categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero tutte le categorie");
            
            // TODO: Implement proper category table
            // For now, return hardcoded categories
            var categories = new List<string>
            {
                "Electronics",
                "Clothing",
                "Home & Garden",
                "Sports",
                "Books",
                "Toys",
                "Food & Beverages"
            };
            
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle categorie");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    /// <param name="categoryName">Category name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of products in the category</returns>
    [HttpGet("~/api/categories/{categoryName}/products")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(
        string categoryName,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero prodotti per categoria: {Category}", categoryName);
            
            // TODO: Implement proper category filtering
            // For now, return all active products
            var products = await _productService.GetAllProductsAsync(ct);
            var result = products.Where(p => p.IsActive).ToList();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti per categoria");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get all brands
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of brands</returns>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetBrands(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero tutti i brand");
            
            // TODO: Implement proper brand table
            // For now, return hardcoded brands
            var brands = new List<string>
            {
                "BrandA",
                "BrandB",
                "BrandC",
                "BrandD",
                "BrandE"
            };
            
            return Ok(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei brand");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get product reviews
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of reviews for the product</returns>
    [HttpGet("{productId:guid}/reviews")]
    [ProducesResponseType(typeof(IEnumerable<ProductReview>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductReview>>> GetProductReviews(
        Guid productId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recupero recensioni per prodotto {ProductId}", productId);
            
            var product = await _productService.GetProductByIdAsync(productId, ct);
            if (product == null)
            {
                return NotFound($"Product with ID {productId} not found");
            }
            
            // TODO: Implement proper review storage
            // For now, return empty list
            var reviews = new List<ProductReview>();
            
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle recensioni");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Add a product review
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="review">Review details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created review</returns>
    [HttpPost("{productId:guid}/reviews")]
    [ProducesResponseType(typeof(ProductReview), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductReview>> AddProductReview(
        Guid productId,
        [FromBody] CreateReviewRequest review,
        CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            _logger.LogInformation("Aggiunta recensione per prodotto {ProductId}", productId);
            
            var product = await _productService.GetProductByIdAsync(productId, ct);
            if (product == null)
            {
                return NotFound($"Product with ID {productId} not found");
            }
            
            // TODO: Implement proper review storage
            var newReview = new ProductReview
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Rating = review.Rating,
                Comment = review.Comment,
                ReviewerName = review.ReviewerName,
                CreatedAt = DateTime.UtcNow
            };
            
            return CreatedAtAction(nameof(GetProductReviews), new { productId }, newReview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta della recensione");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing your request");
        }
    }

    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating an existing product
/// </summary>
public class UpdateProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Product review model
/// </summary>
public class ProductReview
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for creating a product review
/// </summary>
public class CreateReviewRequest
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Comment { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ReviewerName { get; set; } = string.Empty;
}