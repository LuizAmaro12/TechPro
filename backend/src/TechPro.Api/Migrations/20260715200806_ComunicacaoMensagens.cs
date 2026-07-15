using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class ComunicacaoMensagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mensagens_enviadas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: true),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agendamento_id = table.Column<int>(type: "integer", nullable: true),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    destino = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    tipo_evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    assunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    corpo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    erro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    id_externo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensagens_enviadas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mensagens_enviadas_cliente_id",
                table: "mensagens_enviadas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_mensagens_enviadas_ordem_servico_id",
                table: "mensagens_enviadas",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_mensagens_enviadas_tenant_id",
                table: "mensagens_enviadas",
                column: "tenant_id");

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "mensagens_enviadas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mensagens_enviadas");
        }
    }
}
