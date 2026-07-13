using System.Globalization;
using System.Text;

namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Gera o identificador público da loja para a URL de agendamento
/// (/agendar/{slug}) a partir do nome da empresa: minúsculas, sem acentos,
/// apenas [a-z0-9] e hífens simples.
/// </summary>
public static class GeradorDeSlug
{
    public const int TamanhoMaximo = 80;

    public static string Gerar(string nome)
    {
        var semAcentos = nome.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(semAcentos.Length);
        var ultimoFoiHifen = true; // evita hífen no início

        foreach (var caractere in semAcentos)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var minusculo = char.ToLowerInvariant(caractere);
            if (minusculo is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                construtor.Append(minusculo);
                ultimoFoiHifen = false;
            }
            else if (!ultimoFoiHifen)
            {
                construtor.Append('-');
                ultimoFoiHifen = true;
            }
        }

        var slug = construtor.ToString().TrimEnd('-');
        if (slug.Length > TamanhoMaximo)
        {
            slug = slug[..TamanhoMaximo].TrimEnd('-');
        }

        return slug.Length > 0 ? slug : "loja";
    }
}
