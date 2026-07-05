using Microsoft.EntityFrameworkCore.Migrations;

namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Aplica o padrão de Row-Level Security a uma tabela com coluna tenant_id.
/// Chamar em toda migration que criar uma nova tabela de tenant
/// (módulos de produto: clientes, ordens de serviço, estoque, ...).
/// FORCE garante que a policy vale até para o dono da tabela (techpro_app).
/// </summary>
public static class RlsHelper
{
    public static void AplicarIsolamentoTenant(MigrationBuilder migrationBuilder, string tabela) =>
        migrationBuilder.Sql($"""
            ALTER TABLE {tabela} ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {tabela} FORCE ROW LEVEL SECURITY;
            CREATE POLICY {tabela}_isolamento_tenant ON {tabela}
                USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
            """);
}
