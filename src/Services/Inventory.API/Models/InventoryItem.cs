using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.API.Models;

public class InventoryItem
{
    public Guid ProductId { get; set; }
    public int QuantityOnHand { get; set; }
    public int Reserved { get; set; }

    [NotMapped]
    public int Available => QuantityOnHand - Reserved;
}

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public bool Released { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
