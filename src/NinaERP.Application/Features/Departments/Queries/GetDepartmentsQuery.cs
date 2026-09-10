using MediatR;

namespace NinaERP.Application.Features.Departments.Queries;

public record GetDepartmentsQuery(Guid CompanyId) : IRequest<IReadOnlyList<DepartmentResponse>>;

public record DepartmentResponse(
    Guid Id,
    string Name,
    string? Description,
    int ProductCount
);
