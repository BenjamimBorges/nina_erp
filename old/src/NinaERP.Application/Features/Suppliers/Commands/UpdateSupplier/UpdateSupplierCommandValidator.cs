using FluentValidation;

namespace NinaERP.Application.Features.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID é obrigatório");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Documento é obrigatório");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefone é obrigatório");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Endereço é obrigatório");

        RuleFor(x => x.ContactPerson)
            .NotEmpty().WithMessage("Pessoa de contato é obrigatória");
    }
}
