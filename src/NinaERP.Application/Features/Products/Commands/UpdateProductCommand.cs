using NinaERP.Domain.Entities;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using FluentValidation;

namespace NinaERP.Application.Features.Products.Commands;

public record UpdateProductCommand(
    Guid Id, string Name, string Description, string Ncm, string Cest,
    string Unit, decimal PriceSale, decimal PriceMinimum,
    int StockMin, string Brand, string Department, string Barcode
) : IRequest;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PriceSale).GreaterThanOrEqualTo(0);
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

        product.Name = cmd.Name; product.Description = cmd.Description;
        product.Ncm = cmd.Ncm; product.Cest = cmd.Cest;
        product.Unit = cmd.Unit; product.PriceSale = cmd.PriceSale;
        product.PriceMinimum = cmd.PriceMinimum; product.StockMin = cmd.StockMin;
        product.Brand = cmd.Brand; product.Department = cmd.Department;
        product.Barcode = cmd.Barcode; product.UpdatedAt = DateTime.UtcNow;

        _uow.Products.Update(product);
        await _uow.CommitAsync(ct);
    }
}
