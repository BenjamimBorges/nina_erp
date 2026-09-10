using FluentValidation;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Application.Features.Departments.Commands;

public record CreateDepartmentCommand(
    Guid CompanyId,
    string Name,
    string? Description
) : IRequest<Guid>;

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do departamento é obrigatório.")
            .MaximumLength(100).WithMessage("Nome não pode ter mais de 100 caracteres.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descrição não pode ter mais de 500 caracteres.");
    }
}

public sealed class CreateDepartmentHandler : IRequestHandler<CreateDepartmentCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateDepartmentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateDepartmentCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        var existing = await _uow.Departments.GetByNameAsync(cmd.CompanyId, name, ct);
        if (existing is not null)
            throw new BusinessException($"Departamento '{name}' já cadastrado.");

        var department = new Department
        {
            CompanyId = cmd.CompanyId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim()
        };

        await _uow.Departments.AddAsync(department, ct);
        await _uow.CommitAsync(ct);
        return department.Id;
    }
}
