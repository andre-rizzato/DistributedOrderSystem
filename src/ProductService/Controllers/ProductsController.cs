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
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Product>> GetProduct(int id, CancellationToken ct = default)
    {
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("ID prodotto non valido fornito: {ProductId}", id);
                return BadRequest("Product ID must be a positive integer");
            }

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
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] UpdateProductRequest product, CancellationToken ct = default)
    {
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("ID prodotto non valido fornito: {ProductId}", id);
                return BadRequest("Product ID must be a positive integer");
            }

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
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteProduct(int id, CancellationToken ct = default)
    {
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("ID prodotto non valido fornito: {ProductId}", id);
                return BadRequest("Product ID must be a positive integer");
            }

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
}

/// <summary>
/// Request model for creating a new product
/// </summary>
public class CreateProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
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
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}