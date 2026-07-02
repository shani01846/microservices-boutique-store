using MongoDB.Driver;
using ProductService.Models;

namespace ProductService.Data;

public class ProductRepository
{
    private readonly IMongoCollection<Product> _products;

    public ProductRepository(IConfiguration config)
    {
        var client = new MongoClient(config["MongoDB:ConnectionString"]);
        var db = client.GetDatabase(config["MongoDB:DatabaseName"]);
        _products = db.GetCollection<Product>("products");
    }

    public async Task<List<Product>> GetAllAsync() =>
        await _products.Find(_ => true).ToListAsync();

    public async Task<Product?> GetByIdAsync(string id) =>
        await _products.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<Product> CreateAsync(Product product)
    {
        await _products.InsertOneAsync(product);
        return product;
    }

    public async Task UpdateAsync(string id, Product product) =>
        await _products.ReplaceOneAsync(p => p.Id == id, product);

    public async Task DeleteAsync(string id) =>
        await _products.DeleteOneAsync(p => p.Id == id);
}
