using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class OsEKanban : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ordens_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    aparelho_id = table.Column<int>(type: "integer", nullable: true),
                    aparelho_marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    aparelho_modelo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    agendamento_id = table.Column<int>(type: "integer", nullable: true),
                    etapa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    prioridade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prazo_estimado = table.Column<DateOnly>(type: "date", nullable: true),
                    responsavel_tecnico_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_pagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_aprovacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao_problema = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    codigo_acompanhamento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    chave_idempotencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordens_servico", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordens_servico_agendamentos_agendamento_id",
                        column: x => x.agendamento_id,
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_aparelhos_aparelho_id",
                        column: x => x.aparelho_id,
                        principalTable: "aparelhos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_users_responsavel_tecnico_id",
                        column: x => x.responsavel_tecnico_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ordem_servico_historico_etapas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    de_etapa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    para_etapa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordem_servico_historico_etapas", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordem_servico_historico_etapas_ordens_servico_ordem_servico",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_historico_etapas_ordem_servico_id",
                table: "ordem_servico_historico_etapas",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordem_servico_historico_etapas_updated_at",
                table: "ordem_servico_historico_etapas",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_agendamento_id",
                table: "ordens_servico",
                column: "agendamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_aparelho_id",
                table: "ordens_servico",
                column: "aparelho_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_cliente_id",
                table: "ordens_servico",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_codigo_acompanhamento",
                table: "ordens_servico",
                column: "codigo_acompanhamento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_responsavel_tecnico_id",
                table: "ordens_servico",
                column: "responsavel_tecnico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_servico_id",
                table: "ordens_servico",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_tenant_id_chave_idempotencia",
                table: "ordens_servico",
                columns: new[] { "tenant_id", "chave_idempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_tenant_id_etapa",
                table: "ordens_servico",
                columns: new[] { "tenant_id", "etapa" });

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_tenant_id_numero",
                table: "ordens_servico",
                columns: new[] { "tenant_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_updated_at",
                table: "ordens_servico",
                column: "updated_at");

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "ordens_servico");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "ordem_servico_historico_etapas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ordem_servico_historico_etapas");

            migrationBuilder.DropTable(
                name: "ordens_servico");
        }
    }
}
