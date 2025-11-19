import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService } from '../../services/product';
import { Product, CreateProductRequest, UpdateProductRequest } from '../../models/product';

/**
 * ProductFormComponent
 *
 * Componente form per creare e modificare prodotti.
 *
 * Funzionalità:
 * - Crea nuovi prodotti (modalità creazione)
 * - Modifica prodotti esistenti (modalità modifica)
 * - Validazione completa dei campi del form
 * - Messaggi di errore personalizzati per ogni campo
 * - Gestione degli stati di caricamento ed errore
 * - Navigazione automatica alla lista prodotti dopo salvataggio
 *
 * Il componente utilizza Reactive Forms di Angular per la gestione del form,
 * che offre una validazione robusta e una gestione dello stato più strutturata
 * rispetto ai Template-Driven Forms.
 *
 * Modalità:
 * - Se l'URL contiene un ID numerico: modalità MODIFICA (carica prodotto esistente)
 * - Se l'URL contiene 'new': modalità CREAZIONE (form vuoto)
 */
@Component({
  selector: 'app-product-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss'
})
export class ProductFormComponent implements OnInit {
  // FormGroup che gestisce tutti i campi del form e la loro validazione
  productForm: FormGroup;

  // Signal che indica se siamo in modalità modifica (true) o creazione (false)
  isEdit = signal(false);

  // Signal che contiene l'ID del prodotto in modifica (null se in creazione)
  productId = signal<number | null>(null);

  // Signal per gestire lo stato di caricamento
  loading = signal(false);

  // Signal per gestire messaggi di errore
  error = signal<string | null>(null);

  /**
   * Costruttore
   * @param fb - FormBuilder per costruire il form reattivo
   * @param productService - Servizio per le operazioni sui prodotti
   * @param route - ActivatedRoute per leggere i parametri dall'URL
   * @param router - Router per la navigazione programmatica
   */
  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    // Inizializza il form con validatori
    this.productForm = this.fb.group({
      name: ['', [
        Validators.required,           // Campo obbligatorio
        Validators.minLength(1),       // Minimo 1 carattere
        Validators.maxLength(100)      // Massimo 100 caratteri
      ]],
      price: [0, [
        Validators.required,           // Campo obbligatorio
        Validators.min(0.01)           // Deve essere maggiore di 0
      ]],
      description: ['', [
        Validators.maxLength(500)      // Massimo 500 caratteri
      ]],
      isActive: [true]                 // Default: attivo
    });
  }

  /**
   * Lifecycle hook eseguito all'inizializzazione del componente
   * Determina se siamo in modalità modifica o creazione basandosi sull'URL
   */
  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id && id !== 'new') {
      // Modalità MODIFICA: ID numerico presente nell'URL
      this.productId.set(parseInt(id));
      this.isEdit.set(true);
      this.loadProduct();
    }
    // Altrimenti: modalità CREAZIONE (form rimane vuoto)
  }

  /**
   * Carica i dati del prodotto da modificare
   * Popola il form con i dati esistenti del prodotto
   */
  loadProduct(): void {
    const id = this.productId();
    if (!id) return;

    this.loading.set(true);
    this.error.set(null);

    this.productService.getProductById(id).subscribe({
      next: (product) => {
        // Popola il form con i dati del prodotto esistente
        this.productForm.patchValue({
          name: product.name,
          price: product.price,
          description: product.description,
          isActive: product.isActive
        });
        this.loading.set(false);
      },
      error: (error) => {
        this.error.set('Impossibile caricare il prodotto: ' + error.message);
        this.loading.set(false);
        console.error('Errore nel caricamento del prodotto:', error);
      }
    });
  }

  /**
   * Gestisce il submit del form
   * Valida i dati e chiama il servizio appropriato (create o update)
   */
  onSubmit(): void {
    if (this.productForm.invalid) {
      // Se il form non è valido, marca tutti i campi come "toccati"
      // per mostrare tutti gli errori di validazione
      this.markAllFieldsAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const formValue = this.productForm.value;

    if (this.isEdit()) {
      // MODALITÀ MODIFICA: Aggiorna prodotto esistente
      const updateRequest: UpdateProductRequest = {
        name: formValue.name,
        price: formValue.price,
        description: formValue.description,
        isActive: formValue.isActive
      };

      this.productService.updateProduct(this.productId()!, updateRequest).subscribe({
        next: (product) => {
          this.loading.set(false);
          // Naviga alla lista prodotti dopo il successo
          this.router.navigate(['/products']);
        },
        error: (error) => {
          this.error.set('Impossibile aggiornare il prodotto: ' + error.message);
          this.loading.set(false);
          console.error('Errore nell\'aggiornamento del prodotto:', error);
        }
      });
    } else {
      // MODALITÀ CREAZIONE: Crea nuovo prodotto
      const createRequest: CreateProductRequest = {
        name: formValue.name,
        price: formValue.price,
        description: formValue.description
      };

      this.productService.createProduct(createRequest).subscribe({
        next: (product) => {
          this.loading.set(false);
          // Naviga alla lista prodotti dopo il successo
          this.router.navigate(['/products']);
        },
        error: (error) => {
          this.error.set('Impossibile creare il prodotto: ' + error.message);
          this.loading.set(false);
          console.error('Errore nella creazione del prodotto:', error);
        }
      });
    }
  }

  /**
   * Gestisce il click sul pulsante Annulla
   * Torna alla lista prodotti senza salvare
   */
  onCancel(): void {
    this.router.navigate(['/products']);
  }

  /**
   * Marca tutti i campi del form come "toccati"
   * Utile per mostrare tutti gli errori di validazione al submit
   */
  private markAllFieldsAsTouched(): void {
    Object.keys(this.productForm.controls).forEach(key => {
      this.productForm.get(key)?.markAsTouched();
    });
  }

  /**
   * Ottiene il messaggio di errore per un campo specifico
   *
   * Analizza gli errori di validazione e restituisce un messaggio leggibile.
   * Supporta: required, minlength, maxlength, min
   *
   * @param fieldName - Nome del campo da verificare
   * @returns Messaggio di errore o null se il campo è valido
   */
  getFieldError(fieldName: string): string | null {
    const field = this.productForm.get(fieldName);
    if (field && field.invalid && (field.dirty || field.touched)) {
      if (field.errors?.['required']) {
        return `${this.getFieldDisplayName(fieldName)} è obbligatorio`;
      }
      if (field.errors?.['minlength']) {
        return `${this.getFieldDisplayName(fieldName)} deve contenere almeno ${field.errors['minlength'].requiredLength} carattere/i`;
      }
      if (field.errors?.['maxlength']) {
        return `${this.getFieldDisplayName(fieldName)} non può superare ${field.errors['maxlength'].requiredLength} caratteri`;
      }
      if (field.errors?.['min']) {
        return `${this.getFieldDisplayName(fieldName)} deve essere maggiore di ${field.errors['min'].min}`;
      }
    }
    return null;
  }

  /**
   * Converte il nome tecnico del campo in un nome visualizzabile
   * @param fieldName - Nome tecnico del campo (es. 'name', 'price')
   * @returns Nome visualizzabile in italiano (es. 'Nome', 'Prezzo')
   */
  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      name: 'Nome',
      price: 'Prezzo',
      description: 'Descrizione'
    };
    return displayNames[fieldName] || fieldName;
  }

  /**
   * Verifica se un campo è invalido e deve mostrare l'errore
   * @param fieldName - Nome del campo da verificare
   * @returns true se il campo è invalido e stato toccato/modificato
   */
  isFieldInvalid(fieldName: string): boolean {
    const field = this.productForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }
}
