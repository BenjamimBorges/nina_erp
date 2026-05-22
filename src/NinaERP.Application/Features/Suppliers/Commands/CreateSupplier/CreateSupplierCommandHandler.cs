using MediatR;
using NinaERP.Domain.Entities;
using NinaERP.Infrastructure.Repositories.Interfaces;

namespace NinaERP.Application.Features.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly ISupplierRepository _repository;

    public CreateSupplierCommandHandler(ISupplierRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            Document = request.Document,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            ContactPerson = request.ContactPerson,
            CompanyId = request.CompanyId,
            IsActive = true
        };

        await _repository.AddAsync(supplier);
        await _repository.SaveChangesAsync();

        return supplier.Id;
    }
}
