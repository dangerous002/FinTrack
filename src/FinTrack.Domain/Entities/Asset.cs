using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Asset : Entity
{
    public string Name { get; set; } = string.Empty;
    public AssetGroup Group { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal ExtraCosts { get; set; }
    public AssetStatus Status { get; set; }

    public decimal? SalePrice { get; set; }
    public DateOnly? SaleDate { get; set; }
    public int? SaleAccountId { get; set; }
    public Account? SaleAccount { get; set; }
}