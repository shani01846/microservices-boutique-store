namespace ProductService.DTOs;

public record ProductDto(string Id, string Name, string Description, decimal Price, string Category, string Size, string? ImageUrl, int StockQuantity);
public record CreateProductDto(string Name, string Description, decimal Price, string Category, string Size, int StockQuantity);
public record UpdateProductDto(string Name, string Description, decimal Price, string Category, string Size, int StockQuantity);
