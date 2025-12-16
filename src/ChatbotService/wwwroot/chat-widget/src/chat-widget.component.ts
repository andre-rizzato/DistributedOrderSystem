import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { BehaviorSubject, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

export interface ChatMessage {
  id: string;
  text: string;
  isUser: boolean;
  timestamp: Date;
  avatar?: string;
  status?: 'sent' | 'delivered' | 'read' | 'error';
}

export interface ChatConfig {
  // Routing configuration
  useBffRouting: boolean;
  bffBaseUrl?: string;
  chatbotServiceUrl?: string;
  
  // Visual customization
  theme: 'light' | 'dark' | 'auto';
  primaryColor: string;
  secondaryColor: string;
  borderRadius: string;
  position: 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left' | 'center';
  
  // Behavior
  autoOpen: boolean;
  showTypingIndicator: boolean;
  enableSoundNotifications: boolean;
  maxMessages: number;
  
  // Branding
  botName: string;
  botAvatar: string;
  welcomeMessage: string;
  placeholderText: string;
  
  // Authentication
  jwtToken?: string;
  sessionId?: string;
}

@Component({
  selector: 'distributed-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule],
  template: `
    <!-- Chat Widget Trigger Button -->
    <div 
      *ngIf="!isOpen" 
      class="chat-trigger"
      [ngStyle]="getTriggerStyles()"
      (click)="toggleChat()"
      [attr.aria-label]="'Apri chat con ' + config.botName">
      <svg viewBox="0 0 24 24" width="24" height="24">
        <path fill="currentColor" d="M20 2H4C2.9 2 2 2.9 2 4V22L6 18H20C21.1 18 22 17.1 22 16V4C22 2.9 21.1 2 20 2ZM20 16H5.17L4 17.17V4H20V16Z"/>
        <circle cx="8" cy="10" r="1"/>
        <circle cx="12" cy="10" r="1"/>
        <circle cx="16" cy="10" r="1"/>
      </svg>
      <div class="notification-badge" *ngIf="unreadCount > 0">{{unreadCount}}</div>
    </div>

    <!-- Chat Window -->
    <div 
      *ngIf="isOpen" 
      class="chat-window"
      [ngStyle]="getWindowStyles()"
      [ngClass]="'theme-' + config.theme">
      
      <!-- Header -->
      <div class="chat-header" [ngStyle]="getHeaderStyles()">
        <div class="bot-info">
          <img [src]="config.botAvatar" [alt]="config.botName" class="bot-avatar" *ngIf="config.botAvatar">
          <div class="bot-details">
            <span class="bot-name">{{config.botName}}</span>
            <span class="bot-status" [ngClass]="connectionStatus">{{getStatusText()}}</span>
          </div>
        </div>
        <div class="header-actions">
          <button class="minimize-btn" (click)="toggleChat()" aria-label="Minimizza chat">
            <svg viewBox="0 0 24 24" width="20" height="20">
              <path fill="currentColor" d="M19 13H5V11H19V13Z"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- Messages Area -->
      <div class="messages-container" #messagesContainer>
        <div class="welcome-message" *ngIf="messages.length === 0">
          <p>{{config.welcomeMessage}}</p>
        </div>
        
        <div 
          *ngFor="let message of messages; trackBy: trackMessage" 
          class="message"
          [ngClass]="{'user-message': message.isUser, 'bot-message': !message.isUser}">
          
          <div class="message-content" [ngStyle]="getMessageStyles(message)">
            <img 
              *ngIf="!message.isUser && config.botAvatar" 
              [src]="config.botAvatar" 
              class="message-avatar"
              [alt]="config.botName">
            
            <div class="message-bubble">
              <div class="message-text" [innerHTML]="formatMessage(message.text)"></div>
              <div class="message-meta">
                <span class="message-time">{{formatTime(message.timestamp)}}</span>
                <span *ngIf="message.isUser && message.status" class="message-status">
                  <svg *ngIf="message.status === 'sent'" viewBox="0 0 16 16" width="12" height="12">
                    <path fill="currentColor" d="M13.854 3.646a.5.5 0 0 1 0 .708l-7 7a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L6.5 10.293l6.646-6.647a.5.5 0 0 1 .708 0z"/>
                  </svg>
                  <svg *ngIf="message.status === 'delivered'" viewBox="0 0 16 16" width="12" height="12">
                    <path fill="currentColor" d="M12.354 4.354a.5.5 0 0 0-.708-.708L5 10.293 1.854 7.146a.5.5 0 1 0-.708.708l3.5 3.5a.5.5 0 0 0 .708 0l7-7zm-4.208 7-.896-.897.707-.707.543.543 6.646-6.647a.5.5 0 0 1 .708.708l-7 7a.5.5 0 0 1-.708 0z"/>
                  </svg>
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Typing Indicator -->
        <div *ngIf="isTyping && config.showTypingIndicator" class="typing-indicator">
          <div class="typing-dots">
            <span></span>
            <span></span>
            <span></span>
          </div>
          <span class="typing-text">{{config.botName}} sta scrivendo...</span>
        </div>
      </div>

      <!-- Input Area -->
      <div class="chat-input" [ngStyle]="getInputStyles()">
        <div class="input-container">
          <input 
            type="text"
            [(ngModel)]="currentMessage"
            (keypress)="onKeyPress($event)"
            [placeholder]="config.placeholderText"
            [disabled]="isLoading"
            #messageInput
            class="message-input"
            autocomplete="off">
          
          <button 
            class="send-button"
            (click)="sendMessage()"
            [disabled]="!currentMessage.trim() || isLoading"
            [ngStyle]="getSendButtonStyles()"
            aria-label="Invia messaggio">
            <svg *ngIf="!isLoading" viewBox="0 0 24 24" width="20" height="20">
              <path fill="currentColor" d="M2,21L23,12L2,3V10L17,12L2,14V21Z"/>
            </svg>
            <div *ngIf="isLoading" class="loading-spinner"></div>
          </button>
        </div>
        
        <!-- Quick Actions -->
        <div class="quick-actions" *ngIf="quickActions.length > 0">
          <button 
            *ngFor="let action of quickActions"
            class="quick-action-btn"
            (click)="sendQuickAction(action)"
            [ngStyle]="getQuickActionStyles()">
            {{action}}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .chat-trigger {
      position: fixed;
      width: 60px;
      height: 60px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
      transition: all 0.3s ease;
      z-index: 1000;
      color: white;
    }

    .chat-trigger:hover {
      transform: scale(1.1);
      box-shadow: 0 6px 20px rgba(0,0,0,0.2);
    }

    .notification-badge {
      position: absolute;
      top: -5px;
      right: -5px;
      background: #ff4444;
      color: white;
      border-radius: 50%;
      min-width: 20px;
      height: 20px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
      font-weight: bold;
    }

    .chat-window {
      position: fixed;
      width: 380px;
      height: 600px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 10px 30px rgba(0,0,0,0.2);
      display: flex;
      flex-direction: column;
      z-index: 1001;
      overflow: hidden;
      transition: all 0.3s ease;
    }

    .theme-dark .chat-window {
      background: #2d3748;
      color: white;
    }

    .chat-header {
      padding: 16px;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .theme-dark .chat-header {
      border-bottom-color: #4a5568;
    }

    .bot-info {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .bot-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      object-fit: cover;
    }

    .bot-details {
      display: flex;
      flex-direction: column;
    }

    .bot-name {
      font-weight: 600;
      font-size: 16px;
    }

    .bot-status {
      font-size: 12px;
      opacity: 0.7;
    }

    .bot-status.connected {
      color: #48bb78;
    }

    .bot-status.disconnected {
      color: #f56565;
    }

    .minimize-btn {
      background: none;
      border: none;
      padding: 8px;
      border-radius: 6px;
      cursor: pointer;
      opacity: 0.6;
      transition: opacity 0.2s;
    }

    .minimize-btn:hover {
      opacity: 1;
    }

    .messages-container {
      flex: 1;
      overflow-y: auto;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .welcome-message {
      text-align: center;
      padding: 20px;
      opacity: 0.7;
      font-style: italic;
    }

    .message {
      display: flex;
      margin-bottom: 12px;
    }

    .user-message {
      justify-content: flex-end;
    }

    .bot-message {
      justify-content: flex-start;
    }

    .message-content {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      max-width: 80%;
    }

    .message-avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .message-bubble {
      padding: 12px 16px;
      border-radius: 18px;
      word-wrap: break-word;
    }

    .user-message .message-bubble {
      background: #4299e1;
      color: white;
      border-bottom-right-radius: 6px;
    }

    .bot-message .message-bubble {
      background: #f7fafc;
      border-bottom-left-radius: 6px;
    }

    .theme-dark .bot-message .message-bubble {
      background: #4a5568;
    }

    .message-meta {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 4px;
      font-size: 11px;
      opacity: 0.7;
    }

    .typing-indicator {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      font-size: 14px;
      opacity: 0.7;
    }

    .typing-dots {
      display: flex;
      gap: 4px;
    }

    .typing-dots span {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #cbd5e0;
      animation: typing 1.4s infinite;
    }

    .typing-dots span:nth-child(2) {
      animation-delay: 0.2s;
    }

    .typing-dots span:nth-child(3) {
      animation-delay: 0.4s;
    }

    @keyframes typing {
      0%, 60%, 100% { opacity: 0.3; }
      30% { opacity: 1; }
    }

    .chat-input {
      padding: 16px;
      border-top: 1px solid #e2e8f0;
    }

    .theme-dark .chat-input {
      border-top-color: #4a5568;
    }

    .input-container {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .message-input {
      flex: 1;
      padding: 12px 16px;
      border: 1px solid #e2e8f0;
      border-radius: 24px;
      outline: none;
      font-size: 14px;
      transition: border-color 0.2s;
    }

    .message-input:focus {
      border-color: #4299e1;
    }

    .theme-dark .message-input {
      background: #4a5568;
      border-color: #718096;
      color: white;
    }

    .send-button {
      width: 44px;
      height: 44px;
      border: none;
      border-radius: 50%;
      background: #4299e1;
      color: white;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s;
    }

    .send-button:hover:not(:disabled) {
      background: #3182ce;
      transform: scale(1.05);
    }

    .send-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .loading-spinner {
      width: 16px;
      height: 16px;
      border: 2px solid #ffffff30;
      border-top: 2px solid #ffffff;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .quick-actions {
      display: flex;
      gap: 8px;
      margin-top: 12px;
      flex-wrap: wrap;
    }

    .quick-action-btn {
      padding: 8px 12px;
      border: 1px solid #e2e8f0;
      border-radius: 16px;
      background: white;
      font-size: 13px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .quick-action-btn:hover {
      background: #f7fafc;
      border-color: #cbd5e0;
    }

    .theme-dark .quick-action-btn {
      background: #4a5568;
      border-color: #718096;
      color: white;
    }

    .theme-dark .quick-action-btn:hover {
      background: #2d3748;
    }

    /* Responsive */
    @media (max-width: 480px) {
      .chat-window {
        width: 100vw;
        height: 100vh;
        border-radius: 0;
        position: fixed;
        top: 0;
        left: 0;
      }
    }
  `]
})
export class DistributedChatWidgetComponent implements OnInit, OnDestroy {
  @Input() config: ChatConfig = {
    useBffRouting: true,
    bffBaseUrl: '/api/gateway/chat',
    chatbotServiceUrl: '/api/chat',
    theme: 'light',
    primaryColor: '#4299e1',
    secondaryColor: '#f7fafc',
    borderRadius: '12px',
    position: 'bottom-right',
    autoOpen: false,
    showTypingIndicator: true,
    enableSoundNotifications: false,
    maxMessages: 100,
    botName: 'Assistente AI',
    botAvatar: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjAiIGN5PSIyMCIgcj0iMjAiIGZpbGw9IiM0Mjk5ZTEiLz4KPHN2ZyB4PSI4IiB5PSI4IiB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0id2hpdGUiPgo8cGF0aCBkPSJNMTIgMkM2LjQ4IDIgMiA2LjQ4IDIgMTJTNi40OCAxNiAxMiAxNlMxNiA2LjQ4IDIgMTJTNi40OCAyIDEyIDJaTTEyIDE0Ljc1QzE0LjA3IDE0Ljc1IDE1Ljc1IDEzLjA3IDE1Ljc1IDEzLjI1IDE1Ljc1IDExLjE4IDE0LjA3IDkuNSAxMiA5LjVTOC4yNSAxMS4xOCA4LjI1IDEzLjI1IDE5Ljc1IDEyIDEyWk04IDEySDEySDEySDEyaDEyWiIvPgo8L3N2Zz4KPC9zdmc+',
    welcomeMessage: 'Ciao! Come posso aiutarti oggi?',
    placeholderText: 'Scrivi un messaggio...'
  };

  @Output() messageReceived = new EventEmitter<ChatMessage>();
  @Output() messageSent = new EventEmitter<ChatMessage>();
  @Output() chatOpened = new EventEmitter<void>();
  @Output() chatClosed = new EventEmitter<void>();

  messages: ChatMessage[] = [];
  currentMessage = '';
  isOpen = false;
  isLoading = false;
  isTyping = false;
  unreadCount = 0;
  connectionStatus: 'connected' | 'disconnected' = 'disconnected';
  quickActions: string[] = ['Stato ordine', 'Menu prodotti', 'Supporto', 'Orari'];

  private destroy$ = new Subject<void>();
  private sessionId = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.initializeSession();
    this.connectToService();
    
    if (this.config.autoOpen) {
      this.isOpen = true;
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeSession() {
    this.sessionId = this.config.sessionId || this.generateSessionId();
  }

  private generateSessionId(): string {
    return 'chat_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
  }

  private async connectToService() {
    try {
      const baseUrl = this.config.useBffRouting ? this.config.bffBaseUrl : this.config.chatbotServiceUrl;
      const response = await this.http.get(`${baseUrl}/health`).toPromise();
      this.connectionStatus = 'connected';
    } catch {
      this.connectionStatus = 'disconnected';
      setTimeout(() => this.connectToService(), 5000); // Retry after 5 seconds
    }
  }

  toggleChat() {
    this.isOpen = !this.isOpen;
    
    if (this.isOpen) {
      this.unreadCount = 0;
      this.chatOpened.emit();
      
      // Add welcome message if first time opening
      if (this.messages.length === 0) {
        this.addBotMessage(this.config.welcomeMessage);
      }
    } else {
      this.chatClosed.emit();
    }
  }

  async sendMessage(text?: string) {
    const messageText = text || this.currentMessage.trim();
    if (!messageText) return;

    this.currentMessage = '';
    const userMessage = this.addUserMessage(messageText);
    this.messageSent.emit(userMessage);
    
    this.isLoading = true;
    this.isTyping = true;

    try {
      const response = await this.sendToBackend(messageText);
      
      setTimeout(() => {
        this.isTyping = false;
        const botMessage = this.addBotMessage(response.message || 'Mi dispiace, non ho capito.');
        this.messageReceived.emit(botMessage);
        this.isLoading = false;
        
        // Update quick actions if provided
        if (response.quickActions) {
          this.quickActions = response.quickActions;
        }
      }, Math.random() * 1000 + 500); // Simulate typing delay
      
    } catch (error) {
      this.isTyping = false;
      this.isLoading = false;
      this.addBotMessage('Scusa, ho avuto un problema. Riprova tra poco.');
    }
  }

  private async sendToBackend(message: string): Promise<any> {
    const baseUrl = this.config.useBffRouting ? this.config.bffBaseUrl : this.config.chatbotServiceUrl;
    
    const payload = {
      message,
      sessionId: this.sessionId,
      userId: this.config.sessionId || 'anonymous'
    };

    const headers: any = {
      'Content-Type': 'application/json'
    };

    if (this.config.jwtToken) {
      headers['Authorization'] = `Bearer ${this.config.jwtToken}`;
    }

    return this.http.post(`${baseUrl}/analyze`, payload, { headers }).toPromise();
  }

  private addUserMessage(text: string): ChatMessage {
    const message: ChatMessage = {
      id: this.generateMessageId(),
      text,
      isUser: true,
      timestamp: new Date(),
      status: 'sent'
    };
    
    this.messages.push(message);
    this.limitMessages();
    this.scrollToBottom();
    
    return message;
  }

  private addBotMessage(text: string): ChatMessage {
    const message: ChatMessage = {
      id: this.generateMessageId(),
      text,
      isUser: false,
      timestamp: new Date(),
      avatar: this.config.botAvatar
    };
    
    this.messages.push(message);
    this.limitMessages();
    this.scrollToBottom();
    
    if (!this.isOpen) {
      this.unreadCount++;
      if (this.config.enableSoundNotifications) {
        this.playNotificationSound();
      }
    }
    
    return message;
  }

  private generateMessageId(): string {
    return Date.now().toString() + Math.random().toString(36).substr(2, 5);
  }

  private limitMessages() {
    if (this.messages.length > this.config.maxMessages) {
      this.messages = this.messages.slice(-this.config.maxMessages);
    }
  }

  private scrollToBottom() {
    setTimeout(() => {
      const container = document.querySelector('.messages-container');
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    });
  }

  private playNotificationSound() {
    // Simple notification sound using Web Audio API
    try {
      const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      const oscillator = audioContext.createOscillator();
      const gainNode = audioContext.createGain();
      
      oscillator.connect(gainNode);
      gainNode.connect(audioContext.destination);
      
      oscillator.frequency.value = 800;
      oscillator.type = 'sine';
      gainNode.gain.value = 0.1;
      
      oscillator.start();
      oscillator.stop(audioContext.currentTime + 0.1);
    } catch (e) {
      // Fallback: silent notification
    }
  }

  sendQuickAction(action: string) {
    this.sendMessage(action);
  }

  onKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  trackMessage(index: number, message: ChatMessage): string {
    return message.id;
  }

  formatMessage(text: string): string {
    // Simple markdown-like formatting
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/\n/g, '<br>');
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });
  }

  getStatusText(): string {
    return this.connectionStatus === 'connected' ? 'Online' : 'Offline';
  }

  // Styling methods for dynamic customization
  getTriggerStyles() {
    const position = this.config.position;
    const styles: any = {
      backgroundColor: this.config.primaryColor,
      borderRadius: this.config.borderRadius
    };

    switch (position) {
      case 'bottom-right':
        styles.bottom = '20px';
        styles.right = '20px';
        break;
      case 'bottom-left':
        styles.bottom = '20px';
        styles.left = '20px';
        break;
      case 'top-right':
        styles.top = '20px';
        styles.right = '20px';
        break;
      case 'top-left':
        styles.top = '20px';
        styles.left = '20px';
        break;
    }

    return styles;
  }

  getWindowStyles() {
    const position = this.config.position;
    const styles: any = {
      borderRadius: this.config.borderRadius
    };

    switch (position) {
      case 'bottom-right':
        styles.bottom = '90px';
        styles.right = '20px';
        break;
      case 'bottom-left':
        styles.bottom = '90px';
        styles.left = '20px';
        break;
      case 'top-right':
        styles.top = '90px';
        styles.right = '20px';
        break;
      case 'top-left':
        styles.top = '90px';
        styles.left = '20px';
        break;
      case 'center':
        styles.top = '50%';
        styles.left = '50%';
        styles.transform = 'translate(-50%, -50%)';
        break;
    }

    return styles;
  }

  getHeaderStyles() {
    return {
      backgroundColor: this.config.primaryColor,
      color: 'white'
    };
  }

  getMessageStyles(message: ChatMessage) {
    return message.isUser ? {
      flexDirection: 'row-reverse'
    } : {};
  }

  getInputStyles() {
    return {};
  }

  getSendButtonStyles() {
    return {
      backgroundColor: this.config.primaryColor
    };
  }

  getQuickActionStyles() {
    return {
      borderColor: this.config.primaryColor,
      color: this.config.primaryColor
    };
  }
}