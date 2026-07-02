namespace ECommerce.Domain.Entities;

public class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }

    public Product Product { get; set; } = null!;

    public bool CanReserve(int quantity) => QuantityAvailable >= quantity;

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new InvalidOperationException($"Insufficient stock. Available: {QuantityAvailable}, Requested: {quantity}");

        QuantityAvailable -= quantity;
        QuantityReserved += quantity;
    }

    public void Confirm(int quantity) => QuantityReserved -= quantity;

    public void Release(int quantity)
    {
        QuantityAvailable += quantity;
        QuantityReserved -= quantity;
    }
}
