using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class EstoqueBaixaAutomatica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ordem_servico_pecas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    peca_id = table.Column<int>(type: "integer", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    custo_unitario_no_uso = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preco_venda_no_uso = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordem_servico_pecas", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordem_servico_pecas_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ordem_servico_pecas_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_pecas_ordem_servico_id",
                table: "ordem_servico_pecas",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_pecas_peca_id",
                table: "ordem_servico_pecas",
                column: "peca_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_pecas_updated_at",
                table: "ordem_servico_pecas",
                column: "updated_at");

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "ordem_servico_pecas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ordem_servico_pecas");
        }
    }
}
