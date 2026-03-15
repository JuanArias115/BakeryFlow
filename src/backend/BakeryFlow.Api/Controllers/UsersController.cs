using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize(Roles = "Admin,Administrator")]
[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<UserDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<UserDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<UserDto>.Ok(await service.CreateAsync(request, cancellationToken), "Usuario creado."));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<UserDto>.Ok(await service.UpdateAsync(id, request, cancellationToken), "Usuario actualizado."));

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await service.ToggleStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Estado actualizado."));
    }

    [HttpPost("{id:guid}/change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(Guid id, [FromBody] ChangeUserPasswordRequest request, CancellationToken cancellationToken)
    {
        await service.ChangePasswordAsync(id, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Contraseña actualizada."));
    }
}
