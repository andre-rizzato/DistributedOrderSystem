// ShopVerse JavaScript Functions - Funzionalità Interattive Avanzate
// Script principale per la gestione dell'interattività della piattaforma e-commerce

// Inizializzazione principale quando il DOM è completamente caricato
document.addEventListener('DOMContentLoaded', function() {
    console.log('🚀 ShopVerse - Inizializzazione funzionalità...');
    
    // Inizializza tutte le funzionalità principali della piattaforma
    initializeSearch();        // Sistema di ricerca intelligente
    initializeCart();          // Gestione carrello della spesa
    initializeProductCards();  // Interattività cards prodotto
    initializeQuantityControls(); // Controlli quantità prodotti
    
    console.log('✅ ShopVerse - Tutte le funzionalità sono attive!');
});

// ========================================
// SISTEMA DI RICERCA INTELLIGENTE
// ========================================

/**
 * Inizializza il sistema di ricerca con autocomplete avanzato
 * Gestisce suggerimenti in tempo reale e UX ottimizzata
 */
function initializeSearch() {
    const searchInput = document.getElementById('searchInput');
    const searchSuggestions = document.getElementById('searchSuggestions');
    
    if (searchInput) {
        // Event listener per input con debounce per performance ottimali
        searchInput.addEventListener('input', debounce(function() {
            const query = this.value.trim();
            // Avvia ricerca suggerimenti solo con almeno 2 caratteri
            if (query.length >= 2) {
                fetchSearchSuggestions(query);
            } else {
                hideSearchSuggestions();
            }
        }, 300)); // Debounce di 300ms per evitare troppe richieste
        
        // Nasconde suggerimenti quando si clicca fuori dall'area di ricerca
        document.addEventListener('click', function(e) {
            if (!searchInput.contains(e.target) && !searchSuggestions?.contains(e.target)) {
                hideSearchSuggestions();
            }
        });
    }
}

/**
 * Recupera suggerimenti di ricerca dal server tramite API call
 * @param {string} query - Termine di ricerca inserito dall'utente
 */
async function fetchSearchSuggestions(query) {
    try {
        // Chiamata AJAX al controller per ottenere suggerimenti intelligenti
        const response = await fetch(`/Home/SearchSuggestions?query=${encodeURIComponent(query)}`);
        const suggestions = await response.json();
        displaySearchSuggestions(suggestions);
    } catch (error) {
        console.error('❌ ShopVerse - Errore nel caricamento dei suggerimenti:', error);
    }
}

/**
 * Visualizza i suggerimenti di ricerca in un dropdown elegante
 * @param {Array} suggestions - Array di suggerimenti dal server
 */
function displaySearchSuggestions(suggestions) {
    const suggestionsContainer = document.getElementById('searchSuggestions');
    if (!suggestionsContainer) return;
    
    if (suggestions.length === 0) {
        hideSearchSuggestions();
        return;
    }
    
    // Genera HTML per ogni suggerimento con click handler
    suggestionsContainer.innerHTML = suggestions.map(suggestion => 
        `<div class="suggestion-item" onclick="selectSuggestion('${suggestion}')">${suggestion}</div>`
    ).join('');
    
    // Mostra il container dei suggerimenti
    suggestionsContainer.style.display = 'block';
}

/**
 * Nasconde il dropdown dei suggerimenti di ricerca
 */
function hideSearchSuggestions() {
    const suggestionsContainer = document.getElementById('searchSuggestions');
    if (suggestionsContainer) {
        suggestionsContainer.style.display = 'none';
    }
}

/**
 * Gestisce la selezione di un suggerimento da parte dell'utente
 * @param {string} suggestion - Suggerimento selezionato
 */
function selectSuggestion(suggestion) {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.value = suggestion;
        hideSearchSuggestions();
        performSearch(); // Esegue immediatamente la ricerca
    }
}

/**
 * Esegue la ricerca navigando alla pagina risultati
 */
function performSearch() {
    const searchInput = document.getElementById('searchInput');
    if (searchInput && searchInput.value.trim()) {
        // Redirect alla pagina di ricerca con parametri URL
        window.location.href = `/Product/Search?query=${encodeURIComponent(searchInput.value.trim())}`;
    }
}

// Funzionalità carrello
function initializeCart() {
    updateCartBadge();
}

// Aggiungi al carrello
async function addToCart(productId, quantity = 1) {
    try {
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ 
                ProductId: productId, 
                Quantity: quantity 
            })
        });
        
        if (response.ok) {
            const result = await response.json();
            showNotification('Prodotto aggiunto al carrello!', 'success');
            updateCartBadge();
            
            // Animazione button
            const button = event.target;
            button.innerHTML = '<i class="fas fa-check"></i> Aggiunto!';
            button.classList.add('btn-success');
            setTimeout(() => {
                button.innerHTML = '<i class="fas fa-shopping-cart"></i> Aggiungi al carrello';
                button.classList.remove('btn-success');
            }, 2000);
        } else {
            showNotification('Errore nell\'aggiungere il prodotto al carrello', 'error');
        }
    } catch (error) {
        console.error('Errore nell\'aggiungere al carrello:', error);
        showNotification('Errore di connessione', 'error');
    }
}

// Rimuovi dal carrello
async function removeFromCart(productId) {
    try {
        const response = await fetch(`/Cart/RemoveFromCart/${productId}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });
        
        if (response.ok) {
            showNotification('Prodotto rimosso dal carrello', 'success');
            updateCartBadge();
            location.reload(); // Ricarica la pagina carrello
        } else {
            showNotification('Errore nella rimozione del prodotto', 'error');
        }
    } catch (error) {
        console.error('Errore nella rimozione dal carrello:', error);
        showNotification('Errore di connessione', 'error');
    }
}

// Aggiorna quantità nel carrello
async function updateCartQuantity(productId, quantity) {
    try {
        const response = await fetch('/Cart/UpdateQuantity', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ 
                ProductId: productId, 
                Quantity: quantity 
            })
        });
        
        if (response.ok) {
            updateCartBadge();
            updateCartTotals();
        } else {
            showNotification('Errore nell\'aggiornamento della quantità', 'error');
        }
    } catch (error) {
        console.error('Errore nell\'aggiornamento quantità:', error);
    }
}

// Aggiorna badge carrello
async function updateCartBadge() {
    try {
        const response = await fetch('/Cart/GetCartCount');
        if (response.ok) {
            const count = await response.json();
            const badge = document.querySelector('.cart-badge');
            if (badge) {
                badge.textContent = count;
                badge.style.display = count > 0 ? 'inline' : 'none';
            }
        }
    } catch (error) {
        console.error('Errore nell\'aggiornamento del badge carrello:', error);
    }
}

// Aggiorna totali carrello
async function updateCartTotals() {
    try {
        const response = await fetch('/Cart/GetCartTotals');
        if (response.ok) {
            const totals = await response.json();
            document.getElementById('subtotal').textContent = `€${totals.subtotal.toFixed(2)}`;
            document.getElementById('shipping').textContent = totals.shipping > 0 ? `€${totals.shipping.toFixed(2)}` : 'Gratuita';
            document.getElementById('total').textContent = `€${totals.total.toFixed(2)}`;
        }
    } catch (error) {
        console.error('Errore nell\'aggiornamento dei totali:', error);
    }
}

// Inizializza cards prodotti
function initializeProductCards() {
    const productCards = document.querySelectorAll('.product-card');
    productCards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-5px)';
        });
        
        card.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });
}

// Controlli quantità
function initializeQuantityControls() {
    const quantityInputs = document.querySelectorAll('.quantity-input');
    quantityInputs.forEach(input => {
        input.addEventListener('change', function() {
            const productId = this.dataset.productId;
            const quantity = parseInt(this.value);
            if (quantity > 0 && productId) {
                updateCartQuantity(productId, quantity);
            }
        });
    });
}

// Wishlist
async function addToWishlist(productId) {
    try {
        const response = await fetch('/Cart/AddToWishlist', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ ProductId: productId })
        });
        
        if (response.ok) {
            const button = event.target;
            button.classList.toggle('text-danger');
            const isInWishlist = button.classList.contains('text-danger');
            showNotification(
                isInWishlist ? 'Aggiunto alla lista desideri' : 'Rimosso dalla lista desideri', 
                'success'
            );
        }
    } catch (error) {
        console.error('Errore nella gestione wishlist:', error);
    }
}

// Notifiche
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'info'} notification-toast`;
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 1050;
        min-width: 300px;
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        border-radius: 4px;
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.style.opacity = '0';
        setTimeout(() => notification.remove(), 300);
    }, 4000);
}

// Utility functions
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

// Filtri prodotti
function applyFilters() {
    const form = document.getElementById('filtersForm');
    if (form) {
        form.submit();
    }
}

// Ordinamento prodotti
function changeSorting(sortBy) {
    const url = new URL(window.location);
    url.searchParams.set('sortBy', sortBy);
    window.location.href = url.toString();
}

// Paginazione
function goToPage(page) {
    const url = new URL(window.location);
    url.searchParams.set('page', page);
    window.location.href = url.toString();
}

// Zoom immagine prodotto
function initializeImageZoom() {
    const mainImage = document.getElementById('mainProductImage');
    const thumbnails = document.querySelectorAll('.thumbnail-image');
    
    thumbnails.forEach(thumbnail => {
        thumbnail.addEventListener('click', function() {
            mainImage.src = this.src;
            thumbnails.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
        });
    });
}

// Comparazione prodotti
let compareList = [];

function addToCompare(productId) {
    if (compareList.length >= 3) {
        showNotification('Puoi confrontare massimo 3 prodotti', 'warning');
        return;
    }
    
    if (!compareList.includes(productId)) {
        compareList.push(productId);
        updateCompareButton(productId, true);
        showNotification('Prodotto aggiunto al confronto', 'success');
    }
}

function removeFromCompare(productId) {
    compareList = compareList.filter(id => id !== productId);
    updateCompareButton(productId, false);
    showNotification('Prodotto rimosso dal confronto', 'success');
}

function updateCompareButton(productId, isAdded) {
    const button = document.querySelector(`[data-compare-product="${productId}"]`);
    if (button) {
        button.textContent = isAdded ? 'Rimuovi confronto' : 'Confronta';
        button.onclick = isAdded ? 
            () => removeFromCompare(productId) : 
            () => addToCompare(productId);
    }
}

function showComparison() {
    if (compareList.length < 2) {
        showNotification('Seleziona almeno 2 prodotti per confrontarli', 'warning');
        return;
    }
    
    const compareIds = compareList.join(',');
    window.open(`/Product/Compare?productIds=${compareIds}`, '_blank');
}