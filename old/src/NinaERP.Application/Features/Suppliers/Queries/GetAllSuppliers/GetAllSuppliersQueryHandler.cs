using MediatR;
using NinaERP.Contracts.Responses;
using NinaERP.Infrastructure.Repositories.Interfaces;

namespace NinaERP.Application.Features.Suppliers.Queries.GetAllSuppliers;

public class GetAllSuppliersQueryHandler : IRequestHandler<GetAllSuppliersQuery, List<SupplierResponse>>
{
    private readonly ISupplierRepository _repository;

    public GetAllSuppliersQueryHandler(ISupplierRepository repository) => _repository = repository;

    public async Task<List<SupplierResponse>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await _repository.GetAllByCompanyIdAsync(request.CompanyId);

        return suppliers.Select(s => new SupplierResponse
        {
            Id = s.Id,
            Name = s.Name,
            Document = s.Document,
            Email = s.Email,
            Phone = s.Phone,
            Address = s.Address,
            ContactPerson = s.ContactPerson,
            IsActive = s.IsActive,
            CompanyId = s.CompanyId!.Value,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();
