using ERP.Core.Interfaces;
using ERP.Core.Entities;
using ERP.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public ClientsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll()
        {
            var clients = await _uow.Clients.GetAllAsync();
            var dtos = clients.Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Document = c.Document,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CompanyId = c.CompanyId,
                CompanyName = c.Company?.Name ?? string.Empty
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetById(int id)
        {
            var c = await _uow.Clients.GetByIdAsync(id);
            if (c == null) return NotFound();

            var dto = new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Document = c.Document,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CompanyId = c.CompanyId,
                CompanyName = c.Company?.Name ?? string.Empty
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientDto input)
        {
            var entity = new Client
            {
                Name = input.Name,
                Document = input.Document,
                Email = input.Email,
                Phone = input.Phone,
                Address = input.Address,
                CompanyId = input.CompanyId
            };

            await _uow.Clients.AddAsync(entity);
            await _uow.CommitAsync();

            var dto = new ClientDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Document = entity.Document,
                Email = entity.Email,
                Phone = entity.Phone,
                Address = entity.Address,
                CompanyId = entity.CompanyId,
                CompanyName = entity.Company?.Name ?? string.Empty
            };

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDto input)
        {
            var entity = await _uow.Clients.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.Name = input.Name;
            entity.Email = input.Email;
            entity.Phone = input.Phone;
            entity.Address = input.Address;

            _uow.Clients.Update(entity);
            await _uow.CommitAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _uow.Clients.GetByIdAsync(id);
            if (entity == null) return NotFound();

            _uow.Clients.Remove(entity);
            await _uow.CommitAsync();

            return NoContent();
        }
    }
}
