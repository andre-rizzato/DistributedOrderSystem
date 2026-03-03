namespace ProductService.Application.Commands.CreateProduct;

using FluentValidation;

/// <summary>
/// Validatore FluentValidation per CreateProductCommand.
/// Viene eseguito automaticamente dalla ValidationBehavior nella pipeline MediatR,
/// PRIMA che l'handler venga invocato.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Il nome del prodotto è obbligatorio.")
            .MaximumLength(200).WithMessage("Il nome del prodotto non può superare i 200 caratteri.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Il prezzo deve essere maggiore di zero.")
            .LessThan(1_000_000_000).WithMessage("Il prezzo è irragionevolmente alto.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descrizione non può superare i 500 caratteri.")
            .When(x => x.Description is not null);
    }
}
