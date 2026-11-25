namespace FinTrack.Domain.Entities;

public class Goal : Entity
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateOnly Deadline { get; set; }

    /// <summary>Ровно одна из двух привязок: к счёту или к портфелю.</summary>
    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public int? PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
}