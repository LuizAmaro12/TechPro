using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class FundacaoMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "papeis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_papeis", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "papel_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_papel_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_papel_claims_papeis_role_id",
                        column: x => x.role_id,
                        principalTable: "papeis",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tipo_cliente = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revogado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    substituido_por_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuario_claims_usuarios_user_id",
                        column: x => x.user_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_usuario_logins_usuarios_user_id",
                        column: x => x.user_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_papeis",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_papeis", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_usuario_papeis_papeis_role_id",
                        column: x => x.role_id,
                        principalTable: "papeis",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_usuario_papeis_usuarios_user_id",
                        column: x => x.user_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_usuario_tokens_usuarios_user_id",
                        column: x => x.user_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "papeis",
                columns: new[] { "id", "concurrency_stamp", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("a1b0c579-4f6e-4a56-9d10-2f6a1e01a001"), "seed-gestor", "gestor", "GESTOR" },
                    { new Guid("a1b0c579-4f6e-4a56-9d10-2f6a1e01a002"), "seed-tecnico", "tecnico", "TECNICO" },
                    { new Guid("a1b0c579-4f6e-4a56-9d10-2f6a1e01a003"), "seed-atendente", "atendente", "ATENDENTE" }
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "papeis",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_papel_claims_role_id",
                table: "papel_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_usuario_id",
                table: "refresh_tokens",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_claims_user_id",
                table: "usuario_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_logins_user_id",
                table: "usuario_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_papeis_role_id",
                table: "usuario_papeis",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "usuarios",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_tenant_id",
                table: "usuarios",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "usuarios",
                column: "normalized_user_name",
                unique: true);

            // ─── Esqueleto de Row-Level Security (seção 5 do doc de stack) ───
            // Segunda trava do isolamento entre empresas, além do Global Query
            // Filter da aplicação. FORCE aplica as policies até ao dono da
            // tabela (techpro_app, que roda as migrations em dev). NULLIF
            // torna o comportamento fail-closed sem erro de cast quando a
            // variável de sessão app.tenant_id está vazia.
            //
            // "usuarios" e "refresh_tokens" ficam DELIBERADAMENTE fora do RLS:
            // são plano de controle, consultados por chave única antes de
            // existir contexto de tenant (login por e-mail, refresh por token).
            // Toda tabela de MÓDULO DE PRODUTO criada daqui em diante deve
            // chamar RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "tabela").
            migrationBuilder.Sql("""
                ALTER TABLE empresas ENABLE ROW LEVEL SECURITY;
                ALTER TABLE empresas FORCE ROW LEVEL SECURITY;

                CREATE POLICY empresas_isolamento_leitura ON empresas
                    FOR SELECT
                    USING (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);

                CREATE POLICY empresas_cadastro_publico ON empresas
                    FOR INSERT
                    WITH CHECK (true);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropTable(
                name: "papel_claims");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "usuario_claims");

            migrationBuilder.DropTable(
                name: "usuario_logins");

            migrationBuilder.DropTable(
                name: "usuario_papeis");

            migrationBuilder.DropTable(
                name: "usuario_tokens");

            migrationBuilder.DropTable(
                name: "papeis");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
