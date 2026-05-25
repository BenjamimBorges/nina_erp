using MediatR;
using NinaERP.Contracts.Responses;
using NinaERP.Infrastructure.Repositories.Interfaces;

namespace NinaERP.Application.Features.Suppliers.Queries.GetSupplierById;

public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, SupplierResponse>
{
    private readonly ISupplierRepository _repository;

    public GetSupplierByIdQueryHandler(ISupplierRepository repository) => _repository = repository;

    public async Task<SupplierResponse> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(request.Id);
        if (supplier == null)
            throw new InvalidOperationException($"Fornecedor com ID {request.Id} não encontrado");

        return new SupplierResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Document = supplier.Document,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            ContactPerson = supplier.ContactPerson,
            IsActive = supplier.IsActive,
            CompanyId = supplier.CompanyId!.Value,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }
}
