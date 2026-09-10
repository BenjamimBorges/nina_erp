using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NinaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910090000_InitialLegacySchema")]
public sealed class InitialLegacySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(MigrationSqlResource.Read("20260910090000_InitialLegacySchema.sql"), suppressTransaction: false);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Downgrade destrutivo não é automático. Restaure o backup pré-migration validado.");
}
