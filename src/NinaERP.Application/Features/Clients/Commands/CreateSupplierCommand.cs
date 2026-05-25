using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using FluentValidation;

namespace NinaERP.Application.Features.Clients.Commands;

public record CreateSupplierCommand(
    Guid CompanyId, string Document, bool IsLegalEntity, string Name,
    string FantasyName, string StateRegistration, string Email,
    string Phone, string Address, string City, string State,
    string ZipCode, string ContactName
) : IRequest<Guid>;

public class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Document).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateSupplierHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateSupplierCommand cmd, CancellationToken ct)
    {
        var existing = await _uow.Suppliers.GetByDocumentAsync(cmd.CompanyId, cmd.Document, ct);
        if (existing != null)
            throw new BusinessException($"Fornecedor com documento '{cmd.Document}' já cadastrado.");

        var supplier = new Supplier
        {
            CompanyId = cmd.CompanyId, Document = cmd.Document,
            IsLegalEntity = cmd.IsLegalEntity, Name = cmd.Name,
            FantasyName = cmd.FantasyName, StateRegistration = cmd.StateRegistration,
            Email = cmd.Email, Phone = cmd.Phone, Address = cmd.Address,
            City = cmd.City, State = cmd.State, ZipCode = cmd.ZipCode,
            ContactName = cmd.ContactName
        };

        await _uow.Suppliers.AddAsync(supplier, ct);
        await _uow.CommitAsync(ct);
        return supplier.Id;
    }
}
