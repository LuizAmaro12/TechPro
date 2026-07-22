using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class Avaliacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avaliacoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: true),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    responsavel_tecnico_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nota = table.Column<int>(type: "integer", nullable: false),
                    recomendacao = table.Column<int>(type: "integer", nullable: false),
                    comentario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resolvida = table.Column<bool>(type: "boolean", nullable: false),
                    resolucao_nota = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resolvida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolvida_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avaliacoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_avaliacoes_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_avaliacoes_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_avaliacoes_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avaliacoes_cliente_id",
                table: "avaliacoes",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_avaliacoes_ordem_servico_id",
                table: "avaliacoes",
                column: "ordem_servico_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_avaliacoes_servico_id",
                table: "avaliacoes",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_avaliacoes_tenant_id",
                table: "avaliacoes",
                column: "tenant_id");

            // Defesa em profundidade: nenhuma tabela de tenant entra sem RLS
            // (ENABLE + FORCE) além do Global Query Filter do EF.
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "avaliacoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avaliacoes");
        }
    }
}
