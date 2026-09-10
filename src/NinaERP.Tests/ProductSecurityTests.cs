using FluentAssertions;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Application.Features.Products.Commands;
using NinaERP.Domain.Entities;
using NSubstitute;

namespace NinaERP.Tests;

public class ProductSecurityTests
{
    [Fact]
    public async Task Update_Should_Not_Modify_Product_From_Another_Company()
    {
        var product = new Product { CompanyId = Guid.NewGuid(), Sku = "1", Name = "Original" };
        var uow = Substitute.For<IUnitOfWork>();
        uow.Products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var handler = new UpdateProductHandler(uow);

        var command = new UpdateProductCommand(
            Guid.NewGuid(), product.Id, "Alterado", "", "", "", "UN",
            10m, 9m, 1m, "", "", "");

        var action = () => handler.Handle(command, default);
        await action.Should().ThrowAsync<NotFoundException>();
        product.Name.Should().Be("Original");
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
