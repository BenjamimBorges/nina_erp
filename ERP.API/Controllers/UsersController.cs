using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public UsersController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _uow.Users.GetAllAsync();
            var dtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role.ToString(),
                CompanyName = u.Company?.Name ?? string.Empty
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var u = await _uow.Users.GetByIdAsync(id);
            if (u == null) return NotFound();

            return Ok(new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role.ToString(),
                CompanyName = u.Company?.Name ?? string.Empty
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto input)
        {
            var entity = new User
            {
                Username = input.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                FullName = input.FullName,
                Role = input.Role,
                CompanyId = input.CompanyId
            };

            await _uow.Users.AddAsync(entity);
            await _uow.CommitAsync();

            var dto = new UserDto
            {
                Id = entity.Id,
                Username = entity.Username,
                FullName = entity.FullName,
                Role = entity.Role.ToString()
            };
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto input)
        {
            var entity = await _uow.Users.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.FullName = input.FullName;
            entity.Role = input.Role;

            _uow.Users.Update(entity);
            await _uow.CommitAsync();

            return NoContent();
        }

        [HttpPut("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto input)
        {
            var entity = await _uow.Users.GetByIdAsync(id);
            if (entity == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(input.CurrentPassword, entity.PasswordHash))
                return BadRequest(new { message = "Senha atual incorreta." });

            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
            _uow.Users.Update(entity);
            await _uow.CommitAsync();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _uow.Users.GetByIdAsync(id);
            if (entity == null) return NotFound();

            _uow.Users.Remove(entity);
            await _uow.CommitAsync();

            return NoContent();
        }
    }
}
