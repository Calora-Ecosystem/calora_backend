namespace Core.Services.Dashboard.Contracts;

public record GetOverallSummaryDto
{
    public int TotalUsers { get; set; }
    public double TotalUsersGrows { get; set; }
    public int TotalSalesCount { get; set; }
    public double TotalSalesCountGrows { get; set; }
    public long TotalSalesAmount { get; set; }
    public double TotalSalesAmountGrows { get; set; }
}