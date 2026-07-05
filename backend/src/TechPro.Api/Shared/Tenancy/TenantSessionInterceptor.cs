using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Segunda camada do isolamento (defesa em profundidade, seção 5 do doc de stack):
/// em toda conexão aberta, propaga o tenant corrente para a variável de sessão
/// <c>app.tenant_id</c> do Postgres, que as policies de Row-Level Security usam.
/// Sem tenant, grava string vazia — as policies usam NULLIF(..., '') e ficam
/// fail-closed (zero linhas) sem erro de cast.
/// O pool do Npgsql faz DISCARD ALL ao devolver a conexão, então a variável
/// nunca vaza de uma requisição para outra.
/// </summary>
public sealed class TenantSessionInterceptor(ITenantProvider tenantProvider) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var comando = CriarComando(connection);
        comando.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var comando = CriarComando(connection);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CriarComando(DbConnection connection)
    {
        var comando = connection.CreateCommand();
        comando.CommandText = "SELECT set_config('app.tenant_id', @tenant, false)";
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "tenant";
        parametro.Value = tenantProvider.TenantId?.ToString() ?? "";
        comando.Parameters.Add(parametro);
        return comando;
    }
}
