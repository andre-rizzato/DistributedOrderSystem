/**
 * Distributed Chat Widget Library
 * Universal chat widget that can be embedded in any website
 * Supports both BFF routing and direct ChatbotService communication
 */

// Re-export the main component and interfaces
export { DistributedChatWidgetComponent, ChatMessage, ChatConfig } from './chat-widget.component';

// Widget service for programmatic control
export class ChatWidgetService {
  private widgetInstance: any = null;

  init(config: Partial<ChatConfig>, containerId?: string) {
    if (typeof window === 'undefined') return;

    // Load Angular if not already loaded
    this.loadAngular().then(() => {
      this.createWidget(config, containerId);
    });
  }

  private async loadAngular() {
    // Check if Angular is already loaded
    if ((window as any).ng) return;

    // Load Angular core dependencies
    const dependencies = [
      'https://unpkg.com/@angular/core@17/bundles/core.umd.min.js',
      'https://unpkg.com/@angular/common@17/bundles/common.umd.min.js',
      'https://unpkg.com/@angular/forms@17/bundles/forms.umd.min.js',
      'https://unpkg.com/@angular/platform-browser@17/bundles/platform-browser.umd.min.js',
      'https://unpkg.com/@angular/platform-browser-dynamic@17/bundles/platform-browser-dynamic.umd.min.js'
    ];

    for (const url of dependencies) {
      await this.loadScript(url);
    }
  }

  private loadScript(src: string): Promise<void> {
    return new Promise((resolve, reject) => {
      if (document.querySelector(`script[src="${src}"]`)) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = src;
      script.onload = () => resolve();
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }

  private createWidget(config: Partial<ChatConfig>, containerId?: string) {
    const container = containerId 
      ? document.getElementById(containerId)
      : document.body;

    if (!container) {
      console.error('Chat widget container not found');
      return;
    }

    // Create widget element
    const widgetElement = document.createElement('distributed-chat-widget');
    container.appendChild(widgetElement);

    // Initialize with config
    // This would be handled by Angular's component initialization
    console.log('Chat widget initialized with config:', config);
  }

  show() {
    if (this.widgetInstance) {
      this.widgetInstance.isOpen = true;
    }
  }

  hide() {
    if (this.widgetInstance) {
      this.widgetInstance.isOpen = false;
    }
  }

  sendMessage(message: string) {
    if (this.widgetInstance) {
      this.widgetInstance.sendMessage(message);
    }
  }

  updateConfig(config: Partial<ChatConfig>) {
    if (this.widgetInstance) {
      this.widgetInstance.config = { ...this.widgetInstance.config, ...config };
    }
  }
}

// Global widget instance
declare global {
  interface Window {
    DistributedChatWidget: ChatWidgetService;
  }
}

// Initialize global instance
if (typeof window !== 'undefined') {
  window.DistributedChatWidget = new ChatWidgetService();
}

// Default export for module systems
export default ChatWidgetService;