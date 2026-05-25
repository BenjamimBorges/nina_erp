using NinaERP.Domain.Entities;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;

namespace NinaERP.Application.Features.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IUnitOfWork _uow;
    public DeleteProductHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteProductCommand cmd, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Product), cmd.Id);
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        _uow.Products.Update(product);
        await _uow.CommitAsync(ct);
    }
}
