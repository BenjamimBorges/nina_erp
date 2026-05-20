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
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public ProductsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var items = await _uow.Products.GetAllAsync();
            var dtos = items.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Description = p.Description,
                Price = p.Price,
                CompanyId = p.CompanyId,
                CompanyName = p.Company?.Name ?? string.Empty
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var p = await _uow.Products.GetByIdAsync(id);
            if (p == null) return NotFound();

            var dto = new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Description = p.Description,
                Price = p.Price,
                CompanyId = p.CompanyId,
                CompanyName = p.Company?.Name ?? string.Empty
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto input)
        {
            var entity = new Product
            {
                Name = input.Name,
                Sku = input.Sku,
                Description = input.Description,
                Price = input.Price,
                CompanyId = input.CompanyId
            };

            await _uow.Products.AddAsync(entity);
            await _uow.CommitAsync();

            var dto = new ProductDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Sku = entity.Sku,
                Description = entity.Description,
                Price = entity.Price,
                CompanyId = entity.CompanyId,
                CompanyName = entity.Company?.Name ?? string.Empty
            };

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto input)
        {
            var entity = await _uow.Products.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.Name = input.Name;
            entity.Description = input.Description;
            entity.Price = input.Price;

            _uow.Products.Update(entity);
            await _uow.CommitAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _uow.Products.GetByIdAsync(id);
            if (entity == null) return NotFound();

            _uow.Products.Remove(entity);
            await _uow.CommitAsync();

            return NoContent();
        }
    }
}
