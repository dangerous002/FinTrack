using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Transaction : Entity
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    /// <summary>Только для перевода.</summary>
    public int? TargetAccountId { get; set; }
    public Account? TargetAccount { get; set; }

    /// <summary>Только для дохода и расхода.</summary>
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? Note { get; set; }
}