using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Onboarding.Dtos;

namespace TechPro.Api.Modules.Onboarding;

[ApiController]
[Route("api/onboarding")]
[Authorize]
[Produces("application/json")]
public class OnboardingController(OnboardingService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<OnboardingStatusResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Status() => Ok(await service.ObterStatusAsync());

    [HttpPost("concluir")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Concluir()
    {
        await service.ConcluirAsync();
        return NoContent();
    }

    [HttpPost("dados-exemplo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CarregarDadosExemplo()
    {
        await service.CarregarDadosExemploAsync();
        return NoContent();
    }

    [HttpDelete("dados-exemplo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoverDadosExemplo()
    {
        await service.RemoverDadosExemploAsync();
        return NoContent();
    }
}
