using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;

namespace CustomerWebsite.Components;

/// <summary>
/// ViewComponent per la visualizzazione della card prodotto
/// Utilizzato per creare card prodotti riutilizzabili in tutta l'applicazione
/// </summary>
public class ProductCardViewComponent : ViewComponent
{
    /// <summary>
    /// Metodo di invocazione del componente
    /// </summary>
    /// <param name="product">Il prodotto da visualizzare</param>
    /// <returns>View con il modello del prodotto</returns>
    public IViewComponentResult Invoke(ProductDisplayModel product)
    {
        // Validazione del parametro in ingresso
        if (product == null)
        {
            throw new ArgumentNullException(nameof(product), "Il prodotto non può essere null");
        }

        // Passa il modello alla vista del componente
        return View(product);
    }
}
