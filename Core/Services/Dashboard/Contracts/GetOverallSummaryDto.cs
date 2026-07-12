namespace Core.Services.Dashboard.Contracts;

public record GetOverallSummaryDto
{
    public int TotalUsers { get; set; }
    public double TotalUsersGrows { get; set; }
    public int TotalSalesCount { get; set; }
    public double TotalSalesCountGrows { get; set; }
    // So'mda (Amount DB'da tiyinda saqlanadi — /100 qilib qaytariladi).
    public double TotalSalesAmount { get; set; }
    public double TotalSalesAmountGrows { get; set; }
}