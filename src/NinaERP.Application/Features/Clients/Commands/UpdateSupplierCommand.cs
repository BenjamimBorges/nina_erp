using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Application.Features.Clients.Commands;

public record UpdateSupplierCommand(
    Guid Id, string Name, string FantasyName, string StateRegistration,
    string Email, string Phone, string Address, string City,
    string State, string ZipCode, string ContactName
) : IRequest;

public class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateSupplierHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateSupplierCommand cmd, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Supplier), cmd.Id);

        supplier.Name = cmd.Name; supplier.FantasyName = cmd.FantasyName;
        supplier.StateRegistration = cmd.StateRegistration;
        supplier.Email = cmd.Email; supplier.Phone = cmd.Phone;
        supplier.Address = cmd.Address; supplier.City = cmd.City;
        supplier.State = cmd.State; supplier.ZipCode = cmd.ZipCode;
        supplier.ContactName = cmd.ContactName;
        supplier.UpdatedAt = DateTime.UtcNow;

        _uow.Suppliers.Update(supplier);
        await _uow.CommitAsync(ct);
    }
}
