using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<CustomerDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<OptionDto>>>> GetOptions(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<OptionDto>>.Ok(await service.GetOptionsAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustomerDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create([FromBody] SaveCustomerRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustomerDto>.Ok(await service.CreateAsync(request, cancellationToken), "Cliente creado."));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(Guid id, [FromBody] SaveCustomerRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustomerDto>.Ok(await service.UpdateAsync(id, request, cancellationToken), "Cliente actualizado."));

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await service.ToggleStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Estado actualizado."));
    }
}
