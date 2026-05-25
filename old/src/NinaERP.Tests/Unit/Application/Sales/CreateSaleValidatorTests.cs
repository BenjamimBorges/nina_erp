using FluentAssertions;
using NinaERP.Application.Features.Sales.Commands.CreateSale;
using Xunit;

namespace NinaERP.Tests.Unit.Application.Sales;

public class CreateSaleValidatorTests
{
    private readonly CreateSaleCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CreateSaleCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new List<CreateSaleItem>
            {
                new(Guid.NewGuid(), 2, 99.90m)
            }
        );

        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyItems_IsInvalid()
    {
        var command = new CreateSaleCommand(Guid.NewGuid(), Guid.NewGuid(), new List<CreateSaleItem>());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_ZeroQuantity_IsInvalid()
    {
        var command = new CreateSaleCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new List<CreateSaleItem> { new(Guid.NewGuid(), 0, 10m) }
        );

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
