using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using FluentValidation;

namespace NinaERP.Application.Features.Clients.Commands;

public record CreateClientCommand(
    Guid CompanyId, string Document, bool IsLegalEntity, string Name,
    string FantasyName, string Email, string Phone,
    string Address, string City, string State, string ZipCode, decimal CreditLimit
) : IRequest<Guid>;

public class CreateClientValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Document).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}

public class CreateClientHandler : IRequestHandler<CreateClientCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateClientHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateClientCommand cmd, CancellationToken ct)
    {
        var existing = await _uow.Clients.GetByDocumentAsync(cmd.CompanyId, cmd.Document, ct);
        if (existing != null)
            throw new BusinessException($"Documento '{cmd.Document}' já cadastrado.");

        var client = new Client
        {
            CompanyId = cmd.CompanyId, Document = cmd.Document,
            IsLegalEntity = cmd.IsLegalEntity, Name = cmd.Name,
            FantasyName = cmd.FantasyName, Email = cmd.Email,
            Phone = cmd.Phone, Address = cmd.Address,
            City = cmd.City, State = cmd.State,
            ZipCode = cmd.ZipCode, CreditLimit = cmd.CreditLimit
        };

        await _uow.Clients.AddAsync(client, ct);
        await _uow.CommitAsync(ct);
        return client.Id;
    }
}
