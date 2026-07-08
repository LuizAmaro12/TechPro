using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace TechPro.Api.Shared.Api;

public static class ValidacaoExtensions
{
    /// <summary>Converte falhas do FluentValidation no ValidationProblemDetails padrão da API.</summary>
    public static IActionResult ProblemaDeValidacao(this ControllerBase controller, ValidationResult validacao)
    {
        foreach (var erro in validacao.Errors)
        {
            controller.ModelState.AddModelError(erro.PropertyName, erro.ErrorMessage);
        }

        return controller.ValidationProblem(controller.ModelState);
    }
}
