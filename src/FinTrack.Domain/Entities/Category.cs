using FinTrack.Domain.Enums;
using System.Transactions;

namespace FinTrack.Domain.Entities;

public class Category : Entity, IArchivable
{
    public string Name { get; set; } = string.Empty;
    public CategoryKind Kind { get; set; }
    public int? ParentId { get; set; }
    public string Color { get; set; } = "#808080";
    public bool IsArchived { get; set; }

    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];
}