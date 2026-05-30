using MediatR;

namespace NinaERP.Application.Features.Departments.Commands;

public class CreateDepartmentCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
