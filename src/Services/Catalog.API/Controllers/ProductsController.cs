using Asp.Versioning;
using Catalog.API.Data;
using Catalog.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Common.Errors;
using Store.Shared.Common.Security;

namespace Catalog.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("catalog/v{version:apiVersion}/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public ProductsController(CatalogDbContext db) => _db = db;

    /// <summary>Browse products (public). Returns active products unless includeInactive=true (admin view).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> List([FromQuery] bool includeInactive = false)
    {
        var query = _db.Products.AsNoTracking();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Get a single product (public).</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> Get(Guid id)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException($"Product {id} was not found.");

        return Ok(new ProductDto(p.Id, p.Name, p.Price, p.IsActive));
    }

    /// <summary>Create a product (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = StoreRoles.Admin)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest req)
    {
        var product = new Product { Name = req.Name.Trim(), Price = req.Price, IsActive = req.IsActive };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var dto = new ProductDto(product.Id, product.Name, product.Price, product.IsActive);
        return Created($"/catalog/v1/products/{product.Id}", dto);
    }

    /// <summary>Update a product (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = StoreRoles.Admin)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductRequest req)
    {
        var product = await _db.Products.FindAsync(id)
                      ?? throw new NotFoundException($"Product {id} was not found.");

        product.Name = req.Name.Trim();
        product.Price = req.Price;
        product.IsActive = req.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new ProductDto(product.Id, product.Name, product.Price, product.IsActive));
    }
}
