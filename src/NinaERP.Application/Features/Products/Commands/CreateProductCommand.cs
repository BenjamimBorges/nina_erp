using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using FluentValidation;

namespace NinaERP.Application.Features.Products.Commands;

public record CreateProductCommand(
    Guid CompanyId, string Sku, string Name, string Description,
    string Ncm, string Cest, string Unit,
    decimal PriceSale, decimal PriceMinimum, int StockMin,
    string Brand, string Department, string Barcode
) : IRequest<Guid>;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Ncm).Length(8).When(x => !string.IsNullOrEmpty(x.Ncm));
        RuleFor(x => x.PriceSale).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(6);
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateProductHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var existing = await _uow.Products.GetBySkuAsync(cmd.CompanyId, cmd.Sku, ct);
        if (existing != null)
            throw new BusinessException($"SKU '{cmd.Sku}' já cadastrado nesta empresa.");

        var product = new Product
        {
            CompanyId = cmd.CompanyId, Sku = cmd.Sku, Name = cmd.Name,
            Description = cmd.Description, Ncm = cmd.Ncm, Cest = cmd.Cest,
            Unit = cmd.Unit, PriceSale = cmd.PriceSale, PriceMinimum = cmd.PriceMinimum,
            StockMin = cmd.StockMin, Brand = cmd.Brand,
            Department = cmd.Department, Barcode = cmd.Barcode
        };

        await _uow.Products.AddAsync(product, ct);
        await _uow.CommitAsync(ct);
        return product.Id;
    }
}
