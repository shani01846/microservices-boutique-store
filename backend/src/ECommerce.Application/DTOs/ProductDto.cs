namespace ECommerce.Application.DTOs;

public record ProductDto(int Id, string Name, string Description, decimal Price, string Category, string Size, int StockQuantity);

public record CreateProductDto(string Name, string Description, decimal Price, string Category, string Size, int StockQuantity);

public record UpdateProductDto(string Name, string Description, decimal Price, string Category, string Size, int StockQuantity);