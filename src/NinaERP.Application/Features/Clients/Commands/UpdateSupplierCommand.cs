using FluentValidation;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Application.Features.Clients.Commands;

public record UpdateSupplierCommand(
    Guid CompanyId, Guid Id, string Name, string FantasyName, string StateRegistration,
    string Email, string Phone, string Address, string City,
    string State, string ZipCode, string ContactName
) : IRequest;

public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.State).MaximumLength(2);
    }
}

public class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateSupplierHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateSupplierCommand cmd, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Supplier), cmd.Id);

        if (supplier.CompanyId != cmd.CompanyId)
            throw new NotFoundException(nameof(Supplier), cmd.Id);

        supplier.Name = cmd.Name.Trim();
        supplier.FantasyName = cmd.FantasyName?.Trim() ?? string.Empty;
        supplier.StateRegistration = cmd.StateRegistration?.Trim() ?? string.Empty;
        supplier.Email = cmd.Email?.Trim() ?? string.Empty;
        supplier.Phone = cmd.Phone?.Trim() ?? string.Empty;
        supplier.Address = cmd.Address?.Trim() ?? string.Empty;
        supplier.City = cmd.City?.Trim() ?? string.Empty;
        supplier.State = cmd.State?.Trim().ToUpperInvariant() ?? string.Empty;
        supplier.ZipCode = cmd.ZipCode?.Trim() ?? string.Empty;
        supplier.ContactName = cmd.ContactName?.Trim() ?? string.Empty;
        supplier.UpdatedAt = DateTime.UtcNow;

        _uow.Suppliers.Update(supplier);
        await _uow.CommitAsync(ct);
    }
}
