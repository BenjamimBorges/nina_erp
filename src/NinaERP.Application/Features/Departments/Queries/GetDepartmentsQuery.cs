using MediatR;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Departments.Queries;

public class GetDepartmentsQuery : IRequest<IEnumerable<DepartmentResponse>>
{
}

public class DepartmentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}
