namespace MiniShop.Api.Models;
public class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}