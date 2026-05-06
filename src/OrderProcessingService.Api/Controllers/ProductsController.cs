using Microsoft.AspNetCore.Mvc;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Contracts;

namespace OrderProcessingService.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductReadService _products;

    public ProductsController(IProductReadService products)
    {
        _products = products;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _products.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return NotFound(new { error = "Product not found." });

        return Ok(product);
    }
}
