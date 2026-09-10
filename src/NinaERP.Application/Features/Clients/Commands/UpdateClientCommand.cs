using FluentValidation;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Application.Features.Clients.Commands;

public record UpdateClientCommand(
    Guid CompanyId, Guid Id, string Name, string FantasyName, string Email,
    string Phone, string Address, string City, string State,
    string ZipCode, decimal CreditLimit
) : IRequest;

public class UpdateClientValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.State).MaximumLength(2);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}

public class UpdateClientHandler : IRequestHandler<UpdateClientCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateClientHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateClientCommand cmd, CancellationToken ct)
    {
        var client = await _uow.Clients.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Client), cmd.Id);

        if (client.CompanyId != cmd.CompanyId)
            throw new NotFoundException(nameof(Client), cmd.Id);

        client.Name = cmd.Name.Trim();
        client.FantasyName = cmd.FantasyName?.Trim() ?? string.Empty;
        client.Email = cmd.Email?.Trim() ?? string.Empty;
        client.Phone = cmd.Phone?.Trim() ?? string.Empty;
        client.Address = cmd.Address?.Trim() ?? string.Empty;
        client.City = cmd.City?.Trim() ?? string.Empty;
        client.State = cmd.State?.Trim().ToUpperInvariant() ?? string.Empty;
        client.ZipCode = cmd.ZipCode?.Trim() ?? string.Empty;
        client.CreditLimit = cmd.CreditLimit;
        client.UpdatedAt = DateTime.UtcNow;

        _uow.Clients.Update(client);
        await _uow.CommitAsync(ct);
    }
}
