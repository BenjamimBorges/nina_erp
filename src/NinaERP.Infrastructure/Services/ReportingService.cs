using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class ReportingService(AppDbContext db) : IReportingService
{
    public async Task<IReadOnlyList<DailySalesReport>> GetDailySalesAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct)
    {
        var result = new List<DailySalesReport>();
        await using var cmd = Command("SELECT \"Date\", \"GrossSales\", \"Discounts\", \"NetSales\", \"Received\", \"Cancelled\", \"SalesCount\" FROM vw_sales_daily WHERE \"CompanyId\"=@company AND \"Date\">=@from AND \"Date\"<=@to ORDER BY \"Date\" DESC LIMIT @limit", companyId, from, to, limit);
        await ReadAsync(cmd, r => result.Add(new(r.GetDateTime(0), r.GetDecimal(1), r.GetDecimal(2), r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5), r.GetInt32(6))), ct);
        return result;
    }
    public async Task<IReadOnlyList<ProductSalesReport>> GetProductSalesAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct)
    {
        var result = new List<ProductSalesReport>();
        await using var cmd = Command("SELECT \"ProductId\", \"Sku\", \"ProductName\", \"Quantity\", \"GrossSales\", \"NetSales\", \"Cost\", \"Margin\" FROM vw_product_sales WHERE \"CompanyId\"=@company AND \"SaleDate\">=@from AND \"SaleDate\"<=@to ORDER BY \"NetSales\" DESC LIMIT @limit", companyId, from, to, limit);
        await ReadAsync(cmd, r => result.Add(new(r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5), r.GetDecimal(6), r.GetDecimal(7))), ct);
        return result;
    }
    public async Task<IReadOnlyList<CashReport>> GetCashAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct)
    {
        var result = new List<CashReport>();
        await using var cmd = Command("SELECT \"CashSessionId\", \"RegisterName\", \"OpenedAt\", \"ClosedAt\", \"OpeningAmount\", \"Receipts\", \"Withdrawals\", \"Refunds\", \"ExpectedAmount\", \"CountedAmount\", \"Difference\" FROM vw_cash_sessions WHERE \"CompanyId\"=@company AND \"OpenedAt\">=@from AND \"OpenedAt\"<=@to ORDER BY \"OpenedAt\" DESC LIMIT @limit", companyId, from, to, limit);
        await ReadAsync(cmd, r => result.Add(new(r.GetGuid(0), r.GetString(1), r.GetDateTime(2), r.IsDBNull(3) ? null : r.GetDateTime(3), r.GetDecimal(4), r.GetDecimal(5), r.GetDecimal(6), r.GetDecimal(7), r.GetDecimal(8), r.IsDBNull(9) ? null : r.GetDecimal(9), r.IsDBNull(10) ? null : r.GetDecimal(10))), ct);
        return result;
    }
    public async Task<FinancialSummaryReport> GetFinancialSummaryAsync(Guid companyId, CancellationToken ct)
    {
        await using var cmd = Command("SELECT \"PayablesOpen\", \"ReceivablesOpen\", \"PayablesPaid\", \"ReceivablesReceived\", \"OverduePayables\", \"OverdueReceivables\" FROM vw_financial_summary WHERE \"CompanyId\"=@company", companyId, null, null, 1);
        var connection = db.Database.GetDbConnection(); if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct);
        cmd.Connection = connection;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new(r.GetDecimal(0), r.GetDecimal(1), r.GetDecimal(2), r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5)) : new(0,0,0,0,0,0);
    }
    private DbCommand Command(string sql, Guid company, DateTime? from, DateTime? to, int limit)
    {
        var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.CommandText = sql;
        Add(cmd,"@company",company); if(from.HasValue) Add(cmd,"@from",from.Value.ToUniversalTime()); if(to.HasValue) Add(cmd,"@to",to.Value.ToUniversalTime()); if(sql.Contains("@limit")) Add(cmd,"@limit",Math.Clamp(limit,1,5000)); return cmd;
    }
    private static void Add(DbCommand c,string name,object value){var p=c.CreateParameter();p.ParameterName=name;p.Value=value;c.Parameters.Add(p);}
    private async Task ReadAsync(DbCommand cmd, Action<DbDataReader> row, CancellationToken ct){var c=db.Database.GetDbConnection();if(c.State!=System.Data.ConnectionState.Open)await c.OpenAsync(ct);cmd.Connection=c;await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))row(r);}
}
