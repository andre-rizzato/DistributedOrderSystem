using FluentValidation.TestHelper;
using ProductService.Application.Commands.UpdateProduct;
using Xunit;

namespace ProductService.Tests;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Keyboard", 99.90m, "Mechanical keyboard", true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyId_FailsValidation()
    {
        var command = new UpdateProductCommand(Guid.Empty, "Keyboard", 10m, null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Product ID is required.");
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "", 10m, null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name is required.");
    }

    [Fact]
    public void NameLongerThan200Characters_FailsValidation()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), new string('a', 201), 10m, null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name cannot exceed 200 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void PriceNotPositive_FailsValidation(decimal price)
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Product", price, null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than zero.");
    }

    [Fact]
    public void DescriptionLongerThan500Characters_FailsValidation()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Product", 10m, new string('a', 501), true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }
}
