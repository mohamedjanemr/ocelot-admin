import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import type { SignalRNotification } from '../types';

// SignalR Configuration
const SIGNALR_HUB_URL = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5000/configurationHub';

class SignalRService {
  private connection: HubConnection | null = null;
  private listeners: Map<string, ((notification: SignalRNotification) => void)[]> = new Map();
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;

  // Initialize SignalR connection
  async initialize(): Promise<void> {
    if (this.connection?.state === 'Connected') {
      console.log('✅ SignalR already connected');
      return;
    }

    if (this.connection?.state === 'Connecting') {
      console.log('⏳ SignalR already connecting, waiting...');
      return;
    }

    console.log(`🔄 Initializing SignalR connection to: ${SIGNALR_HUB_URL}`);

    try {
      // Stop existing connection if any
      if (this.connection) {
        await this.connection.stop();
      }

      this.connection = new HubConnectionBuilder()
        .withUrl(SIGNALR_HUB_URL, {
          withCredentials: false,
          skipNegotiation: false,
        })
        .withAutomaticReconnect([0, 2000, 10000, 30000])
        .configureLogging(LogLevel.Information)
        .build();

      // Set up event handlers
      this.setupEventHandlers();

      await this.connection.start();
      this.reconnectAttempts = 0;
      console.log('✅ SignalR connection established successfully');

    } catch (error) {
      this.reconnectAttempts++;
      console.error(`❌ SignalR connection failed (attempt ${this.reconnectAttempts}):`, error);
      
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        console.log(`🔄 Retrying SignalR connection in 5 seconds...`);
        setTimeout(() => this.initialize(), 5000);
      } else {
        console.error('❌ Max reconnection attempts reached. SignalR connection failed permanently.');
      }
      
      throw error;
    }
  }

  // Set up SignalR event handlers
  private setupEventHandlers(): void {
    if (!this.connection) return;

    console.log('🔧 Setting up SignalR event handlers');

    // Route Configuration Events
    this.connection.on('RouteConfigCreated', (data: Record<string, unknown>) => {
      console.log('📩 Received: RouteConfigCreated', data);
      this.notifyListeners('RouteConfigCreated', data);
    });

    this.connection.on('RouteConfigUpdated', (data: Record<string, unknown>) => {
      console.log('📩 Received: RouteConfigUpdated', data);
      this.notifyListeners('RouteConfigUpdated', data);
    });

    this.connection.on('RouteConfigDeleted', (data: Record<string, unknown>) => {
      console.log('📩 Received: RouteConfigDeleted', data);
      this.notifyListeners('RouteConfigDeleted', data);
    });

    // Configuration Version Events
    this.connection.on('ConfigurationVersionActivated', (data: Record<string, unknown>) => {
      console.log('📩 Received: ConfigurationVersionActivated', data);
      this.notifyListeners('ConfigurationVersionActivated', data);
    });

    // Connection state events
    this.connection.onreconnecting((error) => {
      console.log('🔄 SignalR reconnecting:', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('✅ SignalR reconnected:', connectionId);
    });

    this.connection.onclose((error) => {
      console.log('❌ SignalR connection closed:', error);
    });
  }

  // Notify all listeners for a specific event type
  private notifyListeners(type: SignalRNotification['type'], data: Record<string, unknown>): void {
    const notification: SignalRNotification = {
      type,
      data,
      timestamp: new Date().toISOString(),
    };

    const typeListeners = this.listeners.get(type) || [];
    const allListeners = this.listeners.get('*') || [];

    [...typeListeners, ...allListeners].forEach(listener => {
      try {
        listener(notification);
      } catch (error) {
        console.error('❌ Error in SignalR listener:', error);
      }
    });
  }

  // Subscribe to specific notification types
  subscribe(
    type: SignalRNotification['type'] | '*', 
    callback: (notification: SignalRNotification) => void
  ): () => void {
    if (!this.listeners.has(type)) {
      this.listeners.set(type, []);
    }

    this.listeners.get(type)!.push(callback);

    // Return unsubscribe function
    return () => {
      const listeners = this.listeners.get(type);
      if (listeners) {
        const index = listeners.indexOf(callback);
        if (index > -1) {
          listeners.splice(index, 1);
        }
      }
    };
  }

  // Disconnect from SignalR hub
  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('✅ SignalR connection stopped');
      } catch (error) {
        console.error('❌ Error stopping SignalR connection:', error);
      } finally {
        this.connection = null;
        this.listeners.clear();
      }
    }
  }

  // Check if connected
  isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  // Get connection state
  getConnectionState(): string {
    return this.connection?.state || 'Disconnected';
  }

  // Send message to hub (if needed for future features)
  async sendMessage(methodName: string, ...args: unknown[]): Promise<void> {
    if (!this.connection || !this.isConnected()) {
      throw new Error('SignalR connection is not established');
    }

    try {
      await this.connection.invoke(methodName, ...args);
    } catch (error) {
      console.error(`❌ Error sending SignalR message (${methodName}):`, error);
      throw error;
    }
  }
}

// Create singleton instance
const signalRService = new SignalRService();

export default signalRService;
export { SignalRService }; 