using FinTrack.Domain.Enums;
using System.Transactions;

namespace FinTrack.Domain.Entities;

public class Account : Entity, IArchivable
{
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }

    /// <summary>Только для вклада.</summary>
    public decimal? InterestRate { get; set; }

    /// <summary>Только для вклада.</summary>
    public int? TermMonths { get; set; }

    public bool IsArchived { get; set; }
    public List<Transaction> Transactions { get; set; } = [];
    public List<Deal> Deals { get; set; } = [];
}