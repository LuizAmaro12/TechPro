using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TechPro.Api.Shared.Tenancy;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class EquipeEAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue TRUE de propósito: o EF gera `false` (default do tipo)
            // e isso deixaria **todos os usuários existentes inativos**, sem
            // conseguir logar. Quem já existe continua ativo.
            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "registros_auditoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    acao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entidade_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    detalhe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros_auditoria", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_tenant_id_entidade",
                table: "registros_auditoria",
                columns: new[] { "tenant_id", "entidade" });

            // usuarios segue fora do RLS por decisao documentada (plano de
            // controle); a trilha de auditoria e dado de tenant e entra normal.
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "registros_auditoria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registros_auditoria");

            migrationBuilder.DropColumn(
                name: "ativo",
                table: "usuarios");
        }
    }
}
