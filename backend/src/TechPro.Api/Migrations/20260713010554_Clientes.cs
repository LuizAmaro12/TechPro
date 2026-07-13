using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class Clientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    vip = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    cliente_principal_id = table.Column<int>(type: "integer", nullable: true),
                    consentiu_comunicacoes = table.Column<bool>(type: "boolean", nullable: false),
                    consentimento_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                    table.ForeignKey(
                        name: "fk_clientes_clientes_cliente_principal_id",
                        column: x => x.cliente_principal_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "aparelhos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modelo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    imei = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    senha_desbloqueio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aparelhos", x => x.id);
                    table.ForeignKey(
                        name: "fk_aparelhos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aparelhos_cliente_id",
                table: "aparelhos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_aparelhos_tenant_id",
                table: "aparelhos",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_cliente_principal_id",
                table: "clientes",
                column: "cliente_principal_id");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_tenant_id",
                table: "clientes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_tenant_id_nome",
                table: "clientes",
                columns: new[] { "tenant_id", "nome" });

            // Segunda camada de isolamento (seção 5 do doc de stack): RLS nas
            // tabelas de tenant do módulo. As policies caem com as tabelas no Down().
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "clientes");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "aparelhos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aparelhos");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
