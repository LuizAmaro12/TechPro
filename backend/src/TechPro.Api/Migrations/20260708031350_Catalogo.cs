using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class Catalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fornecedores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contato = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fornecedores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    preco_base = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    duracao_estimada_minutos = table.Column<int>(type: "integer", nullable: false),
                    prazo_medio_dias = table.Column<int>(type: "integer", nullable: true),
                    exige_diagnostico = table.Column<bool>(type: "boolean", nullable: false),
                    agendavel_online = table.Column<bool>(type: "boolean", nullable: false),
                    capacidade_simultanea = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pecas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    custo_unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preco_venda = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    quantidade_em_estoque = table.Column<int>(type: "integer", nullable: false),
                    estoque_minimo = table.Column<int>(type: "integer", nullable: false),
                    fornecedor_id = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pecas", x => x.id);
                    table.ForeignKey(
                        name: "fk_pecas_fornecedores_fornecedor_id",
                        column: x => x.fornecedor_id,
                        principalTable: "fornecedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "servico_checklist_itens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servico_checklist_itens", x => x.id);
                    table.ForeignKey(
                        name: "fk_servico_checklist_itens_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "servico_pecas",
                columns: table => new
                {
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    peca_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade_padrao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servico_pecas", x => new { x.servico_id, x.peca_id });
                    table.ForeignKey(
                        name: "fk_servico_pecas_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_servico_pecas_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fornecedores_tenant_id",
                table: "fornecedores",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_fornecedor_id",
                table: "pecas",
                column: "fornecedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_tenant_id",
                table: "pecas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_servico_checklist_itens_servico_id",
                table: "servico_checklist_itens",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_servico_checklist_itens_tenant_id",
                table: "servico_checklist_itens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_servico_pecas_peca_id",
                table: "servico_pecas",
                column: "peca_id");

            migrationBuilder.CreateIndex(
                name: "ix_servico_pecas_tenant_id",
                table: "servico_pecas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_servicos_tenant_id",
                table: "servicos",
                column: "tenant_id");

            // Segunda camada de isolamento (seção 5 do doc de stack): RLS em
            // toda tabela de tenant do catálogo. As policies caem junto com as
            // tabelas no Down(), então não precisam de reversão explícita.
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "fornecedores");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "pecas");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "servicos");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "servico_pecas");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "servico_checklist_itens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servico_checklist_itens");

            migrationBuilder.DropTable(
                name: "servico_pecas");

            migrationBuilder.DropTable(
                name: "pecas");

            migrationBuilder.DropTable(
                name: "servicos");

            migrationBuilder.DropTable(
                name: "fornecedores");
        }
    }
}
