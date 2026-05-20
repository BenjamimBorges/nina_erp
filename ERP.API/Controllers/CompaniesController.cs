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
    public class CompaniesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public CompaniesController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
        {
            var items = await _uow.Companies.GetAllAsync();
            var dtos = items.Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                TaxId = c.TaxId,
                Address = c.Address
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyDto>> GetById(int id)
        {
            var c = await _uow.Companies.GetByIdAsync(id);
            if (c == null) return NotFound();

            return Ok(new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                TaxId = c.TaxId,
                Address = c.Address
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyDto input)
        {
            var entity = new Company
            {
                Name = input.Name,
                TaxId = input.TaxId,
                Address = input.Address
            };

            await _uow.Companies.AddAsync(entity);
            await _uow.CommitAsync();

            var dto = new CompanyDto { Id = entity.Id, Name = entity.Name, TaxId = entity.TaxId, Address = entity.Address };
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyDto input)
        {
            var entity = await _uow.Companies.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.Name = input.Name;
            entity.Address = input.Address;

            _uow.Companies.Update(entity);
            await _uow.CommitAsync();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _uow.Companies.GetByIdAsync(id);
            if (entity == null) return NotFound();

            _uow.Companies.Remove(entity);
            await _uow.CommitAsync();

            return NoContent();
        }
    }
}
