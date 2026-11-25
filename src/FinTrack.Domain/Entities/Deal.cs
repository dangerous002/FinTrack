using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Deal : Entity
{
    public int PositionId { get; set; }
    public Position? Position { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public DateOnly Date { get; set; }
    public DealType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}