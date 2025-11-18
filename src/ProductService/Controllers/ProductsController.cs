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
            _logger.LogInformation("Fetching all products");
            var products = await _productService.GetAllProductsAsync(ct);
            
            _logger.LogInformation("Successfully retrieved {ProductCount} products", 
                products?.Count() ?? 0);
            
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all products");
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
                _logger.LogWarning("Invalid product ID provided: {ProductId}", id);
                return BadRequest("Product ID must be a positive integer");
            }

            _logger.LogInformation("Fetching product with ID: {ProductId}", id);
            var product = await _productService.GetProductByIdAsync(id, ct);

            if (product == null)
            {
                _logger.LogInformation("Product with ID {ProductId} not found", id);
                return NotFound($"Product with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved product: {ProductId}", id);
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching product {ProductId}", id);
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
                _logger.LogWarning("Invalid product model provided");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new product: {ProductName}", product.Name);

            var newProduct = new Product
            {
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                IsActive = true
            };

            await _productService.AddProductAsync(newProduct, ct);

            _logger.LogInformation("Successfully created product with ID: {ProductId}", newProduct.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating product");
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
                _logger.LogWarning("Invalid product ID provided: {ProductId}", id);
                return BadRequest("Product ID must be a positive integer");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product model provided for ID: {ProductId}", id);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating product with ID: {ProductId}", id);

            // Check if product exists
            var existingProduct = await _productService.GetProductByIdAsync(id, ct);
            if (existingProduct == null)
            {
                _logger.LogInformation("Product with ID {ProductId} not found for update", id);
                return NotFound($"Product with ID {id} not found");
            }

            // Update product properties
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            existingProduct.IsActive = product.IsActive;

            await _productService.UpdateProductAsync(existingProduct, ct);

            _logger.LogInformation("Successfully updated product with ID: {ProductId}", id);
            return Ok(existingProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating product {ProductId}", id);
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
                _logger.LogWarning("Invalid product ID provided: {ProductId}", id);
                return BadRequest("Product ID must be a positive integer");
            }

            _logger.LogInformation("Deleting product with ID: {ProductId}", id);

            var deleted = await _productService.DeleteProductAsync(id, ct);
            if (!deleted)
            {
                _logger.LogInformation("Product with ID {ProductId} not found for deletion", id);
                return NotFound($"Product with ID {id} not found");
            }

            _logger.LogInformation("Successfully deleted product with ID: {ProductId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting product {ProductId}", id);
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