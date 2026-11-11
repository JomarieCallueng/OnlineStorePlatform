using System.ComponentModel.DataAnnotations;

namespace Catalog.API.Models;

public record ProductDto(Guid Id, string Name, decimal Price, bool IsActive);

public record CreateProductRequest(
    [Required] string Name,
    [Range(0.01, 1_000_000)] decimal Price,
    bool IsActive = true);

public record UpdateProductRequest(
    [Required] string Name,
    [Range(0.01, 1_000_000)] decimal Price,
    bool IsActive);
