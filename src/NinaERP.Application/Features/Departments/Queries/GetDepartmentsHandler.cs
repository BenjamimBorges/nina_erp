using MediatR;
using NinaERP.Application.Common.Interfaces;

namespace NinaERP.Application.Features.Departments.Queries;

public class GetDepartmentsHandler : IRequestHandler<GetDepartmentsQuery, IEnumerable<DepartmentResponse>>
{
    private readonly IUnitOfWork _uow;

    public GetDepartmentsHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<DepartmentResponse>> Handle(GetDepartmentsQuery query, CancellationToken ct)
    {
        var companyId = GetCompanyId();

        var departments = await _uow.Departments.GetByCompanyAsync(companyId, ct);

        var response = departments
            .Where(d => d.IsActive)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                ProductCount = d.Products.Count(p => p.IsActive)
            })
            .OrderBy(d => d.Name)
            .ToList();

        return response;
    }

    private Guid GetCompanyId()
    {
        // Placeholder - será extraído do contexto
        return Guid.Empty;
    }
}

