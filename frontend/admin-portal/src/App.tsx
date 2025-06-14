import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
// import { Toaster } from '@/components/ui/sonner'; // Alternative: shadcn sonner
import Layout from './components/layout/Layout';
import Dashboard from './pages/Dashboard';
import RoutesPage from './pages/Routes';
import { ErrorBoundary } from './components/ui/error-boundary';
import './App.css';

function App() {
  return (
    <ErrorBoundary>
      <Router>
        <div className="App">
          <Layout>
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route path="/routes" element={<RoutesPage />} />
              <Route path="/versions" element={<div className="p-6 text-center text-muted-foreground">Configuration Versions page coming soon...</div>} />
              <Route path="/health" element={<div className="p-6 text-center text-muted-foreground">System Health page coming soon...</div>} />
            </Routes>
          </Layout>
          <Toaster 
            position="top-right"
            toastOptions={{
              duration: 4000,
              className: 'text-sm',
              style: {
                background: 'hsl(var(--card))',
                color: 'hsl(var(--card-foreground))',
                border: '1px solid hsl(var(--border))',
              },
              success: {
                iconTheme: {
                  primary: 'hsl(var(--primary))',
                  secondary: 'hsl(var(--primary-foreground))',
                },
              },
              error: {
                iconTheme: {
                  primary: 'hsl(var(--destructive))',
                  secondary: 'hsl(var(--destructive-foreground))',
                },
              },
            }}
          />
        </div>
      </Router>
    </ErrorBoundary>
  );
}

export default App;
