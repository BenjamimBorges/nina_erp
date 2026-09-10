using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
using NinaERP.Domain.Entities;

namespace NinaERP.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(IUserAdministrationService service) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserAdministrationDto>>>> List(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<UserAdministrationDto>>.Ok(await service.ListAsync(CompanyId, ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var id = await service.CreateAsync(CompanyId,
            new CreateManagedUserDto(request.Username, request.FullName, request.Email, request.Password, request.Role), ct);
        return Created($"/api/users/{id}", ApiResponse<Guid>.Ok(id, "Usuário criado com sucesso."));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeUserRoleRequest request, CancellationToken ct)
    {
        await service.ChangeRoleAsync(CompanyId, UserId, id, request.Role, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetUserActiveRequest request, CancellationToken ct)
    {
        await service.SetActiveAsync(CompanyId, UserId, id, request.IsActive, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetUserPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(CompanyId, id, request.NewPassword, ct);
        return NoContent();
    }
}

public sealed record CreateUserRequest(string Username, string FullName, string Email, string Password, UserRole Role);
public sealed record ChangeUserRoleRequest(UserRole Role);
public sealed record SetUserActiveRequest(bool IsActive);
public sealed record ResetUserPasswordRequest(string NewPassword);
