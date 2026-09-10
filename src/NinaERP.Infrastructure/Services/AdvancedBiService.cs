using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class AdvancedBiService(AppDbContext db) : IAdvancedBiService
{
    public async Task<IReadOnlyList<BiMonthlyDto>> GetMonthlyAsync(Guid companyId, int months, CancellationToken ct)
    {
        var rows = new List<BiMonthlyDto>();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = """
            SELECT "Month","SalesCount","NetSales","Cost","Margin","Received","PayablesOpen","ReceivablesOpen"
            FROM mv_bi_company_monthly
            WHERE "CompanyId"=@company AND "Month">=(date_trunc('month', CURRENT_DATE) - ((@months-1) * interval '1 month'))
            ORDER BY "Month" DESC
            LIMIT 36;
            """;
        Add(cmd, "@company", companyId); Add(cmd, "@months", Math.Clamp(months, 1, 36));
        await ReadAsync(cmd, r => rows.Add(new BiMonthlyDto(r.GetDateTime(0), r.GetInt32(1), r.GetDecimal(2), r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5), r.GetDecimal(6), r.GetDecimal(7))), ct);
        return rows;
    }

    public async Task<IReadOnlyList<BiSellerDto>> GetSellersAsync(Guid companyId, int months, int limit, CancellationToken ct)
    {
        var rows = new List<BiSellerDto>();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = """
            SELECT "UserId","SellerName","Month","SalesCount","NetSales","AverageTicket"
            FROM vw_bi_seller_monthly
            WHERE "CompanyId"=@company AND "Month">=(date_trunc('month', CURRENT_DATE) - ((@months-1) * interval '1 month'))
            ORDER BY "Month" DESC,"NetSales" DESC
            LIMIT @limit;
            """;
        Add(cmd, "@company", companyId); Add(cmd, "@months", Math.Clamp(months, 1, 36)); Add(cmd, "@limit", Math.Clamp(limit, 1, 1000));
        await ReadAsync(cmd, r => rows.Add(new BiSellerDto(r.GetGuid(0), r.GetString(1), r.GetDateTime(2), r.GetInt32(3), r.GetDecimal(4), r.GetDecimal(5))), ct);
        return rows;
    }

    public async Task<IReadOnlyList<BiPaymentMixDto>> GetPaymentMixAsync(Guid companyId, int months, CancellationToken ct)
    {
        var rows = new List<BiPaymentMixDto>();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = """
            SELECT "Month","Method","Transactions","GrossCaptured","Refunded","NetReceived"
            FROM vw_bi_payment_mix_monthly
            WHERE "CompanyId"=@company AND "Month">=(date_trunc('month', CURRENT_DATE) - ((@months-1) * interval '1 month'))
            ORDER BY "Month" DESC,"NetReceived" DESC;
            """;
        Add(cmd, "@company", companyId); Add(cmd, "@months", Math.Clamp(months, 1, 36));
        await ReadAsync(cmd, r => rows.Add(new BiPaymentMixDto(r.GetDateTime(0), r.GetString(1), r.GetInt32(2), r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5))), ct);
        return rows;
    }

    public async Task<BiExecutiveSummaryDto> GetExecutiveSummaryAsync(Guid companyId, int months, CancellationToken ct)
    {
        var monthly = await GetMonthlyAsync(companyId, months, ct);
        var net = monthly.Sum(x => x.NetSales);
        var margin = monthly.Sum(x => x.Margin);
        var received = monthly.Sum(x => x.Received);
        var latest = monthly.OrderByDescending(x => x.Month).FirstOrDefault();
        return new(net, margin, received, latest?.PayablesOpen ?? 0m, latest?.ReceivablesOpen ?? 0m,
            net == 0 ? 0 : decimal.Round(margin / net * 100m, 2), monthly);
    }

    public Task RefreshAsync(CancellationToken ct) => db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY mv_bi_company_monthly;", ct);

    private async Task ReadAsync(DbCommand cmd, Action<DbDataReader> map, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        cmd.Connection = connection;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) map(reader);
    }

    private static void Add(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter(); p.ParameterName = name; p.Value = value; cmd.Parameters.Add(p);
    }
}
