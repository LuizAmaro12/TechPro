using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracoesLojaENotificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "empresas",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco",
                table: "empresas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "politicas",
                table: "empresas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telefone",
                table: "empresas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "preferencias_notificacao",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preferencias_notificacao", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_preferencias_notificacao_tenant_id_tipo_evento_canal",
                table: "preferencias_notificacao",
                columns: new[] { "tenant_id", "tipo_evento", "canal" },
                unique: true);

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "preferencias_notificacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "preferencias_notificacao");

            migrationBuilder.DropColumn(
                name: "email",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "endereco",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "politicas",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "telefone",
                table: "empresas");
        }
    }
}
