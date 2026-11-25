namespace FinTrack.Domain.Entities;

public class Portfolio : Entity, IArchivable 
{
    public string Name { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    public List<Position> Positions { get; set; } = [];
}
