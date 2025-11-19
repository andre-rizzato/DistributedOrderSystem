/**
 * Interfaccia Product
 *
 * Rappresenta un prodotto completo nel sistema.
 * Questa interfaccia viene utilizzata per visualizzare i prodotti esistenti
 * e per ricevere dati dal backend dopo operazioni di creazione/aggiornamento.
 */
export interface Product {
  id: number;           // ID univoco del prodotto (generato dal database)
  name: string;         // Nome del prodotto
  price: number;        // Prezzo del prodotto (decimale)
  description: string;  // Descrizione dettagliata del prodotto
  isActive: boolean;    // Indica se il prodotto è attivo/disponibile
}

/**
 * Interfaccia CreateProductRequest
 *
 * Definisce i dati necessari per creare un nuovo prodotto.
 * Non include l'ID (generato dal server) né isActive (predefinito a true).
 * Viene inviata al backend tramite una richiesta POST.
 */
export interface CreateProductRequest {
  name: string;         // Nome del nuovo prodotto (obbligatorio)
  price: number;        // Prezzo del nuovo prodotto (deve essere > 0)
  description: string;  // Descrizione del nuovo prodotto
}

/**
 * Interfaccia UpdateProductRequest
 *
 * Definisce i dati necessari per aggiornare un prodotto esistente.
 * Include tutti i campi modificabili del prodotto, compreso isActive.
 * Viene inviata al backend tramite una richiesta PUT con l'ID del prodotto nell'URL.
 */
export interface UpdateProductRequest {
  name: string;         // Nome aggiornato del prodotto
  price: number;        // Prezzo aggiornato del prodotto
  description: string;  // Descrizione aggiornata del prodotto
  isActive: boolean;    // Stato attivo/inattivo aggiornato
}
