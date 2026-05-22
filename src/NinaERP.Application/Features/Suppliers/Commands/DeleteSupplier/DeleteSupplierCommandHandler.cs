using MediatR;
using NinaERP.Infrastructure.Repositories.Interfaces;

namespace NinaERP.Application.Features.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand, Unit>
{
    private readonly ISupplierRepository _repository;

    public DeleteSupplierCommandHandler(ISupplierRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(request.Id);
        if (supplier == null)
            throw new InvalidOperationException($"Fornecedor com ID {request.Id} não encontrado");

        await _repository.DeleteAsync(supplier);
        await _repository.SaveChangesAsync();

        return Unit.Value;
    }
}
