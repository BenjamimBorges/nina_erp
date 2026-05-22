using MediatR;
using NinaERP.Infrastructure.Repositories.Interfaces;

namespace NinaERP.Application.Features.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, Unit>
{
    private readonly ISupplierRepository _repository;

    public UpdateSupplierCommandHandler(ISupplierRepository repository) => _repository = repository;

    public async Task<Unit> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(request.Id);
        if (supplier == null)
            throw new InvalidOperationException($"Fornecedor com ID {request.Id} não encontrado");

        supplier.Name = request.Name;
        supplier.Document = request.Document;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.ContactPerson = request.ContactPerson;
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(supplier);
        await _repository.SaveChangesAsync();

        return Unit.Value;
    }
}
