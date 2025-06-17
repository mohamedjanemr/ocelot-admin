import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  Menu,
  Home,
  Settings,
  FileText,
  Activity,
  Wifi,
  X,
  TestTube,
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { useSignalR } from '../../hooks/useSignalR';

const navigation = [
  { name: 'Dashboard', href: '/', icon: Home },
  { name: 'Route Configurations', href: '/routes', icon: Settings },
  { name: 'Configuration Versions', href: '/versions', icon: FileText },
  { name: 'System Health', href: '/health', icon: Activity },
  { name: 'Test Page', href: '/test', icon: TestTube },
];

interface LayoutProps {
  children: React.ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();
  const { connectionState, isConnected } = useSignalR();

  return (
    <div className="min-h-screen bg-background">
      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="fixed inset-0 bg-black/20" onClick={() => setSidebarOpen(false)} />
          <div className="fixed inset-y-0 left-0 z-50 w-64 bg-background border-r shadow-lg">
            {/* Mobile sidebar header */}
            <div className="flex h-16 items-center justify-between px-6 border-b">
              <div className="flex items-center space-x-2">
                <div className="h-8 w-8 bg-primary rounded-lg flex items-center justify-center">
                  <span className="text-lg font-bold text-primary-foreground">O</span>
                </div>
                <span className="text-lg font-semibold">Ocelot</span>
              </div>
              <button
                type="button"
                className="rounded-md p-2 text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors"
                onClick={() => setSidebarOpen(false)}
              >
                <X className="h-5 w-5" />
              </button>
            </div>
            
            {/* Mobile sidebar navigation */}
            <nav className="flex-1 px-4 py-6">
              <ul className="space-y-2">
                {navigation.map((item) => {
                  const Icon = item.icon;
                  return (
                    <li key={item.name}>
                      <Link
                        to={item.href}
                        className={`flex items-center rounded-lg py-2 px-3 text-sm font-medium transition-colors ${
                          location.pathname === item.href
                            ? 'bg-primary text-primary-foreground'
                            : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                        }`}
                        onClick={() => setSidebarOpen(false)}
                      >
                        <Icon className="mr-3 h-4 w-4" />
                        {item.name}
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </nav>
          </div>
        </div>
      )}

      {/* Desktop sidebar */}
      <div className="hidden lg:fixed lg:inset-y-0 lg:z-40 lg:flex lg:w-64 lg:flex-col">
        <div className="flex flex-col overflow-y-auto bg-background border-r">
          {/* Desktop sidebar header */}
          <div className="flex h-16 items-center px-6 border-b">
            <div className="flex items-center space-x-2">
              <div className="h-8 w-8 bg-primary rounded-lg flex items-center justify-center">
                <span className="text-lg font-bold text-primary-foreground">O</span>
              </div>
              <span className="text-xl font-semibold">Ocelot Gateway</span>
            </div>
          </div>
          
          {/* Desktop sidebar navigation */}
          <nav className="flex-1 px-4 py-6">
            <ul className="space-y-2">
              {navigation.map((item) => {
                const Icon = item.icon;
                return (
                  <li key={item.name}>
                    <Link
                      to={item.href}
                      className={`flex items-center rounded-lg py-2 px-3 text-sm font-medium transition-colors ${
                        location.pathname === item.href
                          ? 'bg-primary text-primary-foreground'
                          : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                      }`}
                    >
                      <Icon className="mr-3 h-4 w-4" />
                      {item.name}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </nav>
        </div>
      </div>

      {/* Main content */}
      <div className="lg:pl-64">
        {/* Top header */}
        <header className="sticky top-0 z-30 flex h-16 items-center gap-x-4 bg-background/95 backdrop-blur border-b px-4 sm:px-6 lg:px-8">
          {/* Mobile menu button */}
          <button
            type="button"
            className="p-2 text-muted-foreground lg:hidden hover:bg-accent hover:text-accent-foreground rounded-lg transition-colors"
            onClick={() => setSidebarOpen(true)}
          >
            <Menu className="h-5 w-5" />
          </button>

          <div className="flex flex-1 items-center justify-between">
            <h1 className="text-lg font-semibold text-foreground">
              Gateway Configuration Management
            </h1>
            
            {/* SignalR Connection Status */}
            <Badge variant={isConnected ? "default" : "destructive"} className="gap-1.5">
              <Wifi className="h-3 w-3" />
              <span>{connectionState}</span>
            </Badge>
          </div>
        </header>

        {/* Page content */}
        <main className="p-6 lg:p-8">
          <div className="mx-auto max-w-7xl">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
} 