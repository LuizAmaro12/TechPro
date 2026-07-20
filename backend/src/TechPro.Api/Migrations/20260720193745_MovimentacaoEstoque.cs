using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TechPro.Api.Shared.Tenancy;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class MovimentacaoEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "movimentacoes_estoque",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    peca_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    saldo_apos = table.Column<int>(type: "integer", nullable: false),
                    custo_unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimentacoes_estoque", x => x.id);
                    table.ForeignKey(
                        name: "fk_movimentacoes_estoque_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_movimentacoes_estoque_peca_id",
                table: "movimentacoes_estoque",
                column: "peca_id");

            // Saldo de abertura: sem isto o razão nasceria devendo todo o
            // estoque já existente e a reconciliação (SUM = saldo) — que é a
            // garantia central desta etapa — falharia para dados anteriores.
            //
            // pecas tem FORCE RLS e a migração roda sem tenant na sessão, então
            // o SELECT devolveria zero linhas em silêncio (foi exatamente assim
            // que o backfill do slug das empresas falhou). Desligamos o RLS só
            // durante o backfill. A tabela nova ainda não tem RLS — ela é
            // habilitada logo abaixo, depois do INSERT.
            migrationBuilder.Sql("""
                ALTER TABLE pecas DISABLE ROW LEVEL SECURITY;

                INSERT INTO movimentacoes_estoque
                    (tenant_id, peca_id, tipo, quantidade, saldo_apos,
                     custo_unitario, motivo, criado_em)
                SELECT p.tenant_id, p.id, 'Entrada', p.quantidade_em_estoque,
                       p.quantidade_em_estoque, p.custo_unitario,
                       'Saldo de abertura (antes do registro de movimentações)',
                       now()
                FROM pecas p
                WHERE p.quantidade_em_estoque <> 0;

                ALTER TABLE pecas ENABLE ROW LEVEL SECURITY;
                ALTER TABLE pecas FORCE ROW LEVEL SECURITY;
                """);

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "movimentacoes_estoque");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimentacoes_estoque");
        }
    }
}
