using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class OsProfundidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sla_horas",
                table: "servicos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "etapa_desde",
                table: "ordens_servico",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ordem_servico_comentarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    autor_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordem_servico_comentarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordem_servico_comentarios_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordem_servico_reatribuicoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    de_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    para_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordem_servico_reatribuicoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordem_servico_reatribuicoes_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_comentarios_ordem_servico_id",
                table: "ordem_servico_comentarios",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_comentarios_updated_at",
                table: "ordem_servico_comentarios",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_reatribuicoes_ordem_servico_id",
                table: "ordem_servico_reatribuicoes",
                column: "ordem_servico_id");

            // Defesa em profundidade: além do Global Query Filter do EF, o
            // Postgres isola por tenant (ENABLE + FORCE) — nenhuma tabela nova
            // entra sem isso.
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "ordem_servico_comentarios");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "ordem_servico_reatribuicoes");

            // Sem backfill de etapa_desde: a coluna é anulável de propósito e o
            // cálculo cai para criado_em nas OS antigas. Evita o UPDATE que o
            // FORCE RLS silenciosamente zeraria (lição do backfill de slug).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ordem_servico_comentarios");

            migrationBuilder.DropTable(
                name: "ordem_servico_reatribuicoes");

            migrationBuilder.DropColumn(
                name: "sla_horas",
                table: "servicos");

            migrationBuilder.DropColumn(
                name: "etapa_desde",
                table: "ordens_servico");
        }
    }
}
