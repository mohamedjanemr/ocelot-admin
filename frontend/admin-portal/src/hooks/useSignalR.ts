import { useEffect, useState, useCallback } from 'react';
import signalRService from '../services/signalr';
import type { SignalRNotification } from '../types';

// Hook for SignalR connection management
export function useSignalR() {
  const [connectionState, setConnectionState] = useState<string>('Disconnected');
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const initializeConnection = async () => {
      try {
        await signalRService.initialize();
        setConnectionState(signalRService.getConnectionState());
        setIsConnected(signalRService.isConnected());
      } catch (error) {
        console.error('Failed to initialize SignalR:', error);
      }
    };

    initializeConnection();

    // Check connection state periodically
    const interval = setInterval(() => {
      setConnectionState(signalRService.getConnectionState());
      setIsConnected(signalRService.isConnected());
    }, 5000);

    return () => {
      clearInterval(interval);
      signalRService.disconnect();
    };
  }, []);

  return {
    connectionState,
    isConnected,
  };
}

// Hook for subscribing to SignalR notifications
export function useSignalRNotifications(
  eventType: SignalRNotification['type'] | '*',
  callback: (notification: SignalRNotification) => void,
  dependencies: unknown[] = []
) {
  useEffect(() => {
    const unsubscribe = signalRService.subscribe(eventType, callback);
    return unsubscribe;
  }, [eventType, callback, ...dependencies]);
}

// Hook for route configuration notifications
export function useRouteConfigNotifications(
  onRouteCreated?: (data: Record<string, unknown>) => void,
  onRouteUpdated?: (data: Record<string, unknown>) => void,
  onRouteDeleted?: (data: Record<string, unknown>) => void
) {
  const handleNotification = useCallback((notification: SignalRNotification) => {
    switch (notification.type) {
      case 'RouteConfigCreated':
        onRouteCreated?.(notification.data);
        break;
      case 'RouteConfigUpdated':
        onRouteUpdated?.(notification.data);
        break;
      case 'RouteConfigDeleted':
        onRouteDeleted?.(notification.data);
        break;
    }
  }, [onRouteCreated, onRouteUpdated, onRouteDeleted]);

  useSignalRNotifications('*', handleNotification, [handleNotification]);
}

// Hook for configuration version notifications
export function useConfigurationVersionNotifications(
  onVersionActivated?: (data: Record<string, unknown>) => void
) {
  const handleNotification = useCallback((notification: SignalRNotification) => {
    if (notification.type === 'ConfigurationVersionActivated') {
      onVersionActivated?.(notification.data);
    }
  }, [onVersionActivated]);

  useSignalRNotifications('ConfigurationVersionActivated', handleNotification, [handleNotification]);
} 