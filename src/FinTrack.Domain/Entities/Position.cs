using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Position : Entity, IArchivable
{
    public int PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }

    public string Name { get; set; } = string.Empty;
    public InstrumentType InstrumentType { get; set; }
    public decimal CurrentPrice { get; set; }
    public bool IsArchived { get; set; }

    public List<Deal> Deals { get; set; } = [];
}