using NinaERP.Domain.Entities;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;

namespace NinaERP.Application.Features.Clients.Commands;

public record UpdateClientCommand(
    Guid Id, string Name, string FantasyName, string Email,
    string Phone, string Address, string City, string State,
    string ZipCode, decimal CreditLimit
) : IRequest;

public class UpdateClientHandler : IRequestHandler<UpdateClientCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateClientHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateClientCommand cmd, CancellationToken ct)
    {
        var client = await _uow.Clients.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Client), cmd.Id);

        client.Name = cmd.Name; client.FantasyName = cmd.FantasyName;
        client.Email = cmd.Email; client.Phone = cmd.Phone;
        client.Address = cmd.Address; client.City = cmd.City;
        client.State = cmd.State; client.ZipCode = cmd.ZipCode;
        client.CreditLimit = cmd.CreditLimit;
        client.UpdatedAt = DateTime.UtcNow;

        _uow.Clients.Update(client);
        await _uow.CommitAsync(ct);
    }
}
