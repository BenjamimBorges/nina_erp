using MediatR;
using NinaERP.Application.Common.Exceptions;
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
        var existing = await _uow.Departments.GetByNameAsync(GetCompanyId(), cmd.Name, ct);
        if (existing != null)
            throw new BusinessException($"Departamento '{cmd.Name}' já cadastrado.");

        var department = new Department
        {
            Name = cmd.Name,
            Description = cmd.Description,
            CompanyId = GetCompanyId()
        };

        await _uow.Departments.AddAsync(department, ct);
        await _uow.CommitAsync(ct);

        return department.Id;
    }

    private Guid GetCompanyId()
    {
        // Placeholder - será extraído do JWT no middleware
        return Guid.Empty;
    }
}


