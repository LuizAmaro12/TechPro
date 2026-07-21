using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class FilaEspera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fila_espera",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: true),
                    nome_contato = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefone_contato = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email_contato = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    data_preferida = table.Column<DateOnly>(type: "date", nullable: true),
                    descricao_problema = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    aparelho_marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    aparelho_modelo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolvida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    agendamento_id = table.Column<int>(type: "integer", nullable: true),
                    motivo_descarte = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fila_espera", x => x.id);
                    table.ForeignKey(
                        name: "fk_fila_espera_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fila_espera_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fila_espera_cliente_id",
                table: "fila_espera",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_fila_espera_servico_id",
                table: "fila_espera",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_fila_espera_tenant_id_status",
                table: "fila_espera",
                columns: new[] { "tenant_id", "status" });

            // Defesa em profundidade: além do GQF, o Postgres isola por tenant.
            // Tabela nova e vazia — sem backfill, sem a armadilha do FORCE RLS.
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "fila_espera");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fila_espera");
        }
    }
}
