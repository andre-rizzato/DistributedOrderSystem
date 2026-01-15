import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

/**
 * NotFoundComponent
 * 
 * Componente visualizzato quando l'utente naviga verso una route inesistente (404).
 * Fornisce un'interfaccia user-friendly per gestire errori di navigazione.
 */
@Component({
  selector: 'app-not-found',
  imports: [CommonModule, RouterModule],
  template: `
    <div class="not-found-container">
      <div class="not-found-content">
        <div class="error-icon">🔍</div>
        <h1>404 - Page Not Found</h1>
        <p>The page you're looking for doesn't exist or has been moved.</p>
        
        <div class="suggestions">
          <h3>What you can do:</h3>
          <ul>
            <li>Check the URL for typos</li>
            <li>Go back to the <a routerLink="/products">Products page</a></li>
            <li>Use the navigation menu above</li>
          </ul>
        </div>
        
        <div class="actions">
          <button class="btn btn-primary" routerLink="/products">
            🏠 Go to Products
          </button>
          <button class="btn btn-secondary" (click)="goBack()">
            ← Go Back
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .not-found-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 70vh;
      padding: 2rem;
    }
    
    .not-found-content {
      text-align: center;
      max-width: 600px;
      background: white;
      padding: 3rem;
      border-radius: 1rem;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
    }
    
    .error-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }
    
    h1 {
      color: #dc3545;
      margin-bottom: 1rem;
      font-size: 2rem;
    }
    
    p {
      color: #666;
      margin-bottom: 2rem;
      font-size: 1.1rem;
    }
    
    .suggestions {
      text-align: left;
      margin: 2rem 0;
      padding: 1.5rem;
      background: #f8f9fa;
      border-radius: 0.5rem;
    }
    
    .suggestions h3 {
      margin-bottom: 1rem;
      color: #333;
    }
    
    .suggestions ul {
      margin: 0;
      padding-left: 1.5rem;
    }
    
    .suggestions li {
      margin-bottom: 0.5rem;
    }
    
    .suggestions a {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }
    
    .suggestions a:hover {
      text-decoration: underline;
    }
    
    .actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
    }
    
    .btn {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 0.375rem;
      cursor: pointer;
      text-decoration: none;
      font-size: 1rem;
      font-weight: 500;
      transition: all 0.2s;
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
    }
    
    .btn-primary {
      background-color: #007bff;
      color: white;
    }
    
    .btn-primary:hover {
      background-color: #0056b3;
    }
    
    .btn-secondary {
      background-color: #6c757d;
      color: white;
    }
    
    .btn-secondary:hover {
      background-color: #545b62;
    }
    
    @media (max-width: 768px) {
      .not-found-content {
        padding: 2rem;
      }
      
      .actions {
        flex-direction: column;
      }
      
      .btn {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class NotFoundComponent {
  /**
   * Torna alla pagina precedente usando la cronologia del browser
   */
  goBack(): void {
    try {
      window.history.back();
    } catch (error) {
      console.error('Error navigating back:', error);
      // Fallback: redirect to products if history.back() fails
      window.location.href = '/products';
    }
  }
}