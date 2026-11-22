namespace FinTrack.Domain.Entities;

public interface IArchivable
{
    bool IsArchived { get; set; }
}