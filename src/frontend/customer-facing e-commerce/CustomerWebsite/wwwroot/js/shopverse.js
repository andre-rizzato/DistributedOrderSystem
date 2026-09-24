// ShopVerse JavaScript Functions - Advanced Interactive Features
// Main script for handling the e-commerce platform's interactivity

// Main initialization once the DOM is fully loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('🚀 ShopVerse - Initializing features...');

    // Initialize all of the platform's main features
    initializeSearch();        // Smart search system
    initializeCart();          // Shopping cart management
    initializeProductCards();  // Product card interactivity
    initializeQuantityControls(); // Product quantity controls

    console.log('✅ ShopVerse - All features are active!');
});

// ========================================
// SMART SEARCH SYSTEM
// ========================================

/**
 * Initializes the search system with advanced autocomplete
 * Handles real-time suggestions and an optimized UX
 */
function initializeSearch() {
    const searchInput = document.getElementById('searchInput');
    const searchSuggestions = document.getElementById('searchSuggestions');

    if (searchInput) {
        // Input listener with debounce for optimal performance
        searchInput.addEventListener('input', debounce(function() {
            const query = this.value.trim();
            // Only trigger suggestion search with at least 2 characters
            if (query.length >= 2) {
                fetchSearchSuggestions(query);
            } else {
                hideSearchSuggestions();
            }
        }, 300)); // 300ms debounce to avoid too many requests

        // Hide suggestions when clicking outside the search area
        document.addEventListener('click', function(e) {
            if (!searchInput.contains(e.target) && !searchSuggestions?.contains(e.target)) {
                hideSearchSuggestions();
            }
        });
    }
}

/**
 * Fetches search suggestions from the server via an API call
 * @param {string} query - Search term entered by the user
 */
async function fetchSearchSuggestions(query) {
    try {
        // AJAX call to the controller to get smart suggestions
        const response = await fetch(`/Home/SearchSuggestions?query=${encodeURIComponent(query)}`);
        const suggestions = await response.json();
        displaySearchSuggestions(suggestions);
    } catch (error) {
        console.error('❌ ShopVerse - Error loading suggestions:', error);
    }
}

/**
 * Displays search suggestions in a stylish dropdown
 * @param {Array} suggestions - Array of suggestions from the server
 */
function displaySearchSuggestions(suggestions) {
    const suggestionsContainer = document.getElementById('searchSuggestions');
    if (!suggestionsContainer) return;

    if (suggestions.length === 0) {
        hideSearchSuggestions();
        return;
    }

    // Generate HTML for each suggestion with a click handler
    suggestionsContainer.innerHTML = suggestions.map(suggestion =>
        `<div class="suggestion-item" onclick="selectSuggestion('${suggestion}')">${suggestion}</div>`
    ).join('');

    // Show the suggestions container
    suggestionsContainer.style.display = 'block';
}

/**
 * Hides the search suggestions dropdown
 */
function hideSearchSuggestions() {
    const suggestionsContainer = document.getElementById('searchSuggestions');
    if (suggestionsContainer) {
        suggestionsContainer.style.display = 'none';
    }
}

/**
 * Handles the user selecting a suggestion
 * @param {string} suggestion - Selected suggestion
 */
function selectSuggestion(suggestion) {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.value = suggestion;
        hideSearchSuggestions();
        performSearch(); // Immediately run the search
    }
}

/**
 * Runs the search by navigating to the results page
 */
function performSearch() {
    const searchInput = document.getElementById('searchInput');
    if (searchInput && searchInput.value.trim()) {
        // Redirect to the search page with URL parameters
        window.location.href = `/Product/Search?query=${encodeURIComponent(searchInput.value.trim())}`;
    }
}

// Cart functionality
function initializeCart() {
    updateCartBadge();
}

// Add to cart
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
            showNotification('Product added to cart!', 'success');
            updateCartBadge();

            // Button animation
            const button = event.target;
            button.innerHTML = '<i class="fas fa-check"></i> Added!';
            button.classList.add('btn-success');
            setTimeout(() => {
                button.innerHTML = '<i class="fas fa-shopping-cart"></i> Add to cart';
                button.classList.remove('btn-success');
            }, 2000);
        } else {
            showNotification('Error adding the product to the cart', 'error');
        }
    } catch (error) {
        console.error('Error adding to cart:', error);
        showNotification('Connection error', 'error');
    }
}

// Remove from cart
async function removeFromCart(productId) {
    try {
        const response = await fetch(`/Cart/RemoveFromCart/${productId}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });

        if (response.ok) {
            showNotification('Product removed from cart', 'success');
            updateCartBadge();
            location.reload(); // Reload the cart page
        } else {
            showNotification('Error removing the product', 'error');
        }
    } catch (error) {
        console.error('Error removing from cart:', error);
        showNotification('Connection error', 'error');
    }
}

// Update cart quantity
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
            showNotification('Error updating the quantity', 'error');
        }
    } catch (error) {
        console.error('Error updating quantity:', error);
    }
}

// Update cart badge
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
        console.error('Error updating the cart badge:', error);
    }
}

// Update cart totals
async function updateCartTotals() {
    try {
        const response = await fetch('/Cart/GetCartTotals');
        if (response.ok) {
            const totals = await response.json();
            document.getElementById('subtotal').textContent = `€${totals.subtotal.toFixed(2)}`;
            document.getElementById('shipping').textContent = totals.shipping > 0 ? `€${totals.shipping.toFixed(2)}` : 'Free';
            document.getElementById('total').textContent = `€${totals.total.toFixed(2)}`;
        }
    } catch (error) {
        console.error('Error updating the totals:', error);
    }
}

// Initialize product cards
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

// Quantity controls
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
                isInWishlist ? 'Added to wishlist' : 'Removed from wishlist',
                'success'
            );
        }
    } catch (error) {
        console.error('Error handling the wishlist:', error);
    }
}

// Notifications
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

// Product filters
function applyFilters() {
    const form = document.getElementById('filtersForm');
    if (form) {
        form.submit();
    }
}

// Product sorting
function changeSorting(sortBy) {
    const url = new URL(window.location);
    url.searchParams.set('sortBy', sortBy);
    window.location.href = url.toString();
}

// Pagination
function goToPage(page) {
    const url = new URL(window.location);
    url.searchParams.set('page', page);
    window.location.href = url.toString();
}

// Product image zoom
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

// Product comparison
let compareList = [];

function addToCompare(productId) {
    if (compareList.length >= 3) {
        showNotification('You can compare up to 3 products', 'warning');
        return;
    }

    if (!compareList.includes(productId)) {
        compareList.push(productId);
        updateCompareButton(productId, true);
        showNotification('Product added to comparison', 'success');
    }
}

function removeFromCompare(productId) {
    compareList = compareList.filter(id => id !== productId);
    updateCompareButton(productId, false);
    showNotification('Product removed from comparison', 'success');
}

function updateCompareButton(productId, isAdded) {
    const button = document.querySelector(`[data-compare-product="${productId}"]`);
    if (button) {
        button.textContent = isAdded ? 'Remove from comparison' : 'Compare';
        button.onclick = isAdded ?
            () => removeFromCompare(productId) :
            () => addToCompare(productId);
    }
}

function showComparison() {
    if (compareList.length < 2) {
        showNotification('Select at least 2 products to compare them', 'warning');
        return;
    }

    const compareIds = compareList.join(',');
    window.open(`/Product/Compare?productIds=${compareIds}`, '_blank');
}
