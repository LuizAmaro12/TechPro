using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TechPro.Api.Shared.Tenancy;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class TemplatesMensagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "templates_mensagem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_evento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    corpo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_templates_mensagem", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_templates_mensagem_tenant_id_tipo_evento",
                table: "templates_mensagem",
                columns: new[] { "tenant_id", "tipo_evento" },
                unique: true);

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "templates_mensagem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "templates_mensagem");
        }
    }
}
