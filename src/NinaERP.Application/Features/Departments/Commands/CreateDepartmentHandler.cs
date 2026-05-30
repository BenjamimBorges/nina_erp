using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Application.Features.Departments.Commands;

public class CreateDepartmentHandler : IRequestHandler<CreateDepartmentCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateDepartmentHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateDepartmentCommand cmd, CancellationToken ct)
    {
        var department = new Department
        {
            Name = cmd.Name,
            Description = cmd.Description,
            CompanyId = GetCompanyId()
        };

        _uow.Departments.Add(department);
        await _uow.CommitAsync(ct);

        return department.Id;
    }

    private Guid GetCompanyId()
    {
        // Placeholder - será extraído do JWT no middleware
        return Guid.Empty;
    }
}

