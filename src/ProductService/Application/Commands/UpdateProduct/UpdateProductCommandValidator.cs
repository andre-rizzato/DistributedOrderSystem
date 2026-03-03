namespace ProductService.Application.Commands.UpdateProduct;

using FluentValidation;

/// <summary>
/// Validatore FluentValidation per UpdateProductCommand.
/// </summary>
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("L'ID del prodotto è obbligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Il nome del prodotto è obbligatorio.")
            .MaximumLength(200).WithMessage("Il nome del prodotto non può superare i 200 caratteri.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Il prezzo deve essere maggiore di zero.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descrizione non può superare i 500 caratteri.")
            .When(x => x.Description is not null);
    }
}
