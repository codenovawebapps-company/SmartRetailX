using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _db;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductDbContext db, ILogger<ProductsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/products
    /// Returns all products, optionally filtered by category or search term.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Description.ToLower().Contains(s));
        }

        var products = await query.ToListAsync();
        return Ok(products);
    }

    /// <summary>
    /// GET /api/v1/products/{id}
    /// Retrieves a single product by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProductById(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }
        return Ok(product);
    }

    /// <summary>
    /// POST /api/v1/products
    /// Creates a new product catalog item.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name) || product.Price < 0)
        {
            return BadRequest(new { message = "Valid product name and positive price are required." });
        }

        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    /// <summary>
    /// PUT /api/v1/products/{id}
    /// Updates an existing product catalog item.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] Product updatedProduct)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        if (!string.IsNullOrWhiteSpace(updatedProduct.Name)) product.Name = updatedProduct.Name;
        if (!string.IsNullOrWhiteSpace(updatedProduct.Description)) product.Description = updatedProduct.Description;
        if (!string.IsNullOrWhiteSpace(updatedProduct.Category)) product.Category = updatedProduct.Category;
        if (!string.IsNullOrWhiteSpace(updatedProduct.ImageUrl)) product.ImageUrl = updatedProduct.ImageUrl;
        if (updatedProduct.Price >= 0) product.Price = updatedProduct.Price;
        if (updatedProduct.Stock >= 0) product.Stock = updatedProduct.Stock;

        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(product);
    }

    /// <summary>
    /// DELETE /api/v1/products/{id}
    /// Deletes a product from the catalog.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Product with ID {id} successfully deleted." });
    }
}
