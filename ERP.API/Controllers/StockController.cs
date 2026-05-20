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
    public class StockController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public StockController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockDto>>> GetAll()
        {
            var items = await _uow.Stocks.GetAllAsync();
            var dtos = items.Select(s => new StockDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? string.Empty,
                Quantity = s.Quantity,
                Location = s.Location,
                CompanyId = s.CompanyId
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StockDto>> GetById(int id)
        {
            var s = await _uow.Stocks.GetByIdAsync(id);
            if (s == null) return NotFound();

            var dto = new StockDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? string.Empty,
                Quantity = s.Quantity,
                Location = s.Location,
                CompanyId = s.CompanyId
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<StockDto>> Create([FromBody] CreateStockDto input)
        {
            var entity = new Stock
            {
                ProductId = input.ProductId,
                Quantity = input.Quantity,
                Location = input.Location,
                CompanyId = input.CompanyId
            };

            await _uow.Stocks.AddAsync(entity);
            await _uow.CommitAsync();

            var dto = new StockDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = entity.Product?.Name ?? string.Empty,
                Quantity = entity.Quantity,
                Location = entity.Location,
                CompanyId = entity.CompanyId
            };

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStockDto input)
        {
            var entity = await _uow.Stocks.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.Quantity = input.Quantity;
            entity.Location = input.Location;

            _uow.Stocks.Update(entity);
            await _uow.CommitAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _uow.Stocks.GetByIdAsync(id);
            if (entity == null) return NotFound();

            _uow.Stocks.Remove(entity);
            await _uow.CommitAsync();

            return NoContent();
        }
    }
}
