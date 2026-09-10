using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NinaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910210000_Stages20To30")]
public sealed class Stages20To30 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(MigrationSqlResource.Read("20260910210000_Stages20To30.sql"), suppressTransaction: false);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Downgrade destrutivo das Etapas 20–30 não é automático. Restaure o backup pré-migration validado.");
}
