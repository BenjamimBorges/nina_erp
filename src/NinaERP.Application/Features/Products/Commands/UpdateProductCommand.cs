using FluentValidation;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Application.Features.Products.Commands;

public record UpdateProductCommand(
    Guid CompanyId, Guid Id, string Name, string Description, string Ncm, string Cest,
    string Unit, decimal PriceSale, decimal PriceMinimum,
    decimal StockMin, string Brand, string Department, string Barcode
) : IRequest;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Ncm).Length(8).When(x => !string.IsNullOrWhiteSpace(x.Ncm));
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(6);
        RuleFor(x => x.PriceSale).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceMinimum).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockMin).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateProductHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Product), cmd.Id);

        if (product.CompanyId != cmd.CompanyId)
            throw new NotFoundException(nameof(Product), cmd.Id);

        product.Name = cmd.Name.Trim();
        product.Description = cmd.Description?.Trim() ?? string.Empty;
        product.Ncm = cmd.Ncm?.Trim() ?? string.Empty;
        product.Cest = cmd.Cest?.Trim() ?? string.Empty;
        product.Unit = cmd.Unit.Trim().ToUpperInvariant();
        product.PriceSale = cmd.PriceSale;
        product.PriceMinimum = cmd.PriceMinimum;
        product.StockMin = cmd.StockMin;
        product.Brand = cmd.Brand?.Trim() ?? string.Empty;
        product.Department = cmd.Department?.Trim() ?? string.Empty;
        product.Barcode = cmd.Barcode?.Trim() ?? string.Empty;
        product.UpdatedAt = DateTime.UtcNow;

        _uow.Products.Update(product);
        await _uow.CommitAsync(ct);
    }
}
