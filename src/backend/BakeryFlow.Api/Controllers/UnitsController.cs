using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Units;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class UnitsController(IUnitService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UnitDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<UnitDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<OptionDto>>>> GetOptions(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<OptionDto>>.Ok(await service.GetOptionsAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UnitDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<UnitDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UnitDto>>> Create([FromBody] SaveUnitRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<UnitDto>.Ok(await service.CreateAsync(request, cancellationToken), "Unidad creada."));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UnitDto>>> Update(Guid id, [FromBody] SaveUnitRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<UnitDto>.Ok(await service.UpdateAsync(id, request, cancellationToken), "Unidad actualizada."));

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await service.ToggleStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Estado actualizado."));
    }
}
