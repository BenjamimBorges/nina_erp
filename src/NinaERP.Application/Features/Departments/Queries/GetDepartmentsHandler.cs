using MediatR;
using NinaERP.Application.Common.Interfaces;

namespace NinaERP.Application.Features.Departments.Queries;

public sealed class GetDepartmentsHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentResponse>>
{
    private readonly IUnitOfWork _uow;

    public GetDepartmentsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<DepartmentResponse>> Handle(GetDepartmentsQuery query, CancellationToken ct)
    {
        var departments = await _uow.Departments.GetByCompanyAsync(query.CompanyId, ct);
        return departments
            .Where(d => d.IsActive)
            .Select(d => new DepartmentResponse(
                d.Id,
                d.Name,
                d.Description,
                d.Products.Count(p => p.IsActive)))
            .OrderBy(d => d.Name)
            .ToList();
    }
}
