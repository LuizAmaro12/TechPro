using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChecklistTecnicoOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "itens_checklist_ordem_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    concluido = table.Column<bool>(type: "boolean", nullable: false),
                    concluido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concluido_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_checklist_ordem_servico", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_checklist_ordem_servico_ordens_servico_ordem_servico_",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_itens_checklist_ordem_servico_ordem_servico_id",
                table: "itens_checklist_ordem_servico",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_checklist_ordem_servico_updated_at",
                table: "itens_checklist_ordem_servico",
                column: "updated_at");

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "itens_checklist_ordem_servico");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_checklist_ordem_servico");
        }
    }
}
