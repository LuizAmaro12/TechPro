using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechPro.Api.Shared.Tenancy;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgendaEPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "empresas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            // Backfill de empresas existentes antes do índice único: slug
            // provisório derivado do id (a loja edita depois em configurações).
            // O RLS FORCE vale até para o dono da tabela e ainda não existe
            // policy de UPDATE — sem o desliga/liga, o UPDATE afetaria zero
            // linhas em silêncio e o índice único falharia nos slugs vazios.
            migrationBuilder.Sql("""
                ALTER TABLE empresas DISABLE ROW LEVEL SECURITY;
                UPDATE empresas SET slug = 'loja-' || substr(id::text, 1, 8) WHERE slug = '';
                ALTER TABLE empresas ENABLE ROW LEVEL SECURITY;
                ALTER TABLE empresas FORCE ROW LEVEL SECURITY;
                """);

            migrationBuilder.CreateTable(
                name: "agendamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: true),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome_contato = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefone_contato = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email_contato = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    descricao_problema = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    aparelho_marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    aparelho_modelo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reagendado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agendamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_agendamentos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agendamentos_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bloqueios_agenda",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bloqueios_agenda", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "horarios_funcionamento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia_semana = table.Column<int>(type: "integer", nullable: false),
                    abertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    fechamento = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    intervalo_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    intervalo_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_horarios_funcionamento", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_empresas_slug",
                table: "empresas",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_cliente_id",
                table: "agendamentos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_servico_id",
                table: "agendamentos",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_tenant_id_data",
                table: "agendamentos",
                columns: new[] { "tenant_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ix_bloqueios_agenda_tenant_id_data",
                table: "bloqueios_agenda",
                columns: new[] { "tenant_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ix_horarios_funcionamento_tenant_id_dia_semana",
                table: "horarios_funcionamento",
                columns: new[] { "tenant_id", "dia_semana" },
                unique: true);

            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "agendamentos");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "bloqueios_agenda");
            RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "horarios_funcionamento");

            // A rota pública de agendamento resolve slug → empresa antes de
            // existir tenant na sessão. A tabela empresas é só diretório
            // (id, nome, slug, criado_em — nenhum segredo de tenant), então a
            // leitura vira pública; os dados sensíveis seguem nas tabelas com
            // isolamento por tenant. Também nasce aqui a policy de UPDATE da
            // própria empresa (edição do slug) — até então nenhuma existia e o
            // FORCE RLS negaria o UPDATE silenciosamente.
            migrationBuilder.Sql("""
                DROP POLICY empresas_isolamento_leitura ON empresas;

                CREATE POLICY empresas_leitura_publica ON empresas
                    FOR SELECT
                    USING (true);

                CREATE POLICY empresas_atualizacao_propria ON empresas
                    FOR UPDATE
                    USING (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agendamentos");

            migrationBuilder.DropTable(
                name: "bloqueios_agenda");

            migrationBuilder.DropTable(
                name: "horarios_funcionamento");

            migrationBuilder.DropIndex(
                name: "ix_empresas_slug",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "empresas");
        }
    }
}
