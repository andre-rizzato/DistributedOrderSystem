using FluentValidation.TestHelper;
using ProductService.Application.Commands.CreateProduct;
using Xunit;

namespace ProductService.Tests;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = new CreateProductCommand("Keyboard", 99.90m, "Mechanical keyboard");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidCommand_WithNullDescription_PassesValidation()
    {
        var command = new CreateProductCommand("Mouse", 49.90m, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_FailsValidation(string name)
    {
        var command = new CreateProductCommand(name, 10m, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name is required.");
    }

    [Fact]
    public void NameLongerThan200Characters_FailsValidation()
    {
        var command = new CreateProductCommand(new string('a', 201), 10m, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name cannot exceed 200 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PriceNotPositive_FailsValidation(decimal price)
    {
        var command = new CreateProductCommand("Product", price, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than zero.");
    }

    [Fact]
    public void PriceTooHigh_FailsValidation()
    {
        var command = new CreateProductCommand("Product", 1_000_000_000m, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price is unreasonably high.");
    }

    [Fact]
    public void DescriptionLongerThan500Characters_FailsValidation()
    {
        var command = new CreateProductCommand("Product", 10m, new string('a', 501));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }
}
