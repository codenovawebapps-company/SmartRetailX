using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using System.Collections.Concurrent;

namespace ProductService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly ConcurrentDictionary<int, Product> _products = new();
    private static int _nextId = 0;

    static ProductsController()
    {
        var p1 = new Product { Id = 1, Name = "Laptop", Description = "High performance laptop", Price = 999.99m, Category = "Electronics" };
        var p2 = new Product { Id = 2, Name = "Headphones", Description = "Noise-cancelling headphones", Price = 199.99m, Category = "Electronics" };
        _products[p1.Id] = p1;
        _products[p2.Id] = p2;
        _nextId = 2;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetProducts()
    {
        return Ok(_products.Values);
    }

    [HttpGet("{id}")]
    public ActionResult<Product> GetProductById(int id)
    {
        if (_products.TryGetValue(id, out var product))
        {
            return Ok(product);
        }
        return NotFound(new { message = $"Product with ID {id} not found." });
    }

    [HttpPost]
    public ActionResult<Product> CreateProduct([FromBody] Product product)
    {
        if (product.Id <= 0)
        {
            product.Id = Interlocked.Increment(ref _nextId);
        }
        else
        {
            int currentId;
            do
            {
                currentId = _nextId;
                if (product.Id < currentId) break;
            } while (Interlocked.CompareExchange(ref _nextId, product.Id + 1, currentId) != currentId);
        }

        _products[product.Id] = product;
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public ActionResult<Product> UpdateProduct(int id, [FromBody] Product updatedProduct)
    {
        if (!_products.ContainsKey(id))
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        updatedProduct.Id = id;
        _products[id] = updatedProduct;
        return Ok(updatedProduct);
    }
}
