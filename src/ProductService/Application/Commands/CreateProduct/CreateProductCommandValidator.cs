namespace ProductService.Application.Commands.CreateProduct;

using FluentValidation;

/// <summary>
/// FluentValidation validator for CreateProductCommand.
/// Runs automatically via ValidationBehavior in the MediatR pipeline,
/// BEFORE the handler is invoked.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .LessThan(1_000_000_000).WithMessage("Price is unreasonably high.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
