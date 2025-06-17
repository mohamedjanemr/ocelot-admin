import { useState } from 'react';
import {
  CheckCircle,
  AlertCircle,
  Info,
  Zap,
  Users,
  Clock,
  TrendingUp,
  Settings,
  Shield,
  Activity,
  RefreshCw,
  Download,
  Upload,
  Eye,
  Edit3,
  Trash2,
  Plus,
} from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';
import { LoadingState } from '@/components/ui/loading-state';
import { EmptyState } from '@/components/ui/empty-state';
import { ErrorBoundary } from '@/components/ui/error-boundary';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

export default function TestPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [showEmpty, setShowEmpty] = useState(false);
  const [showError, setShowError] = useState(false);

  const handleToggleLoading = () => {
    setIsLoading(!isLoading);
  };

  const handleToggleEmpty = () => {
    setShowEmpty(!showEmpty);
  };

  const handleToggleError = () => {
    setShowError(!showError);
  };

  const mockData = [
    {
      id: 1,
      name: 'Test API Route',
      path: '/api/test',
      method: 'GET',
      status: 'active',
      lastTested: '2 minutes ago'
    },
    {
      id: 2,
      name: 'Authentication Test',
      path: '/api/auth/login',
      method: 'POST',
      status: 'warning',
      lastTested: '5 minutes ago'
    },
    {
      id: 3,
      name: 'Health Check',
      path: '/health',
      method: 'GET',
      status: 'error',
      lastTested: '1 hour ago'
    }
  ];

  const stats = [
    {
      title: 'Total Tests',
      value: 42,
      description: 'Automated test cases',
      icon: CheckCircle,
      change: '+12%',
      changeType: 'positive' as const,
      color: 'text-green-600',
      bgColor: 'bg-green-50',
    },
    {
      title: 'Passing Tests',
      value: 38,
      description: 'Currently passing',
      icon: Shield,
      change: '+5%',
      changeType: 'positive' as const,
      color: 'text-blue-600',
      bgColor: 'bg-blue-50',
    },
    {
      title: 'Failed Tests',
      value: 3,
      description: 'Needs attention',
      icon: AlertCircle,
      change: '-2',
      changeType: 'negative' as const,
      color: 'text-red-600',
      bgColor: 'bg-red-50',
    },
    {
      title: 'Coverage',
      value: '87%',
      description: 'Code coverage',
      icon: Activity,
      change: '+3%',
      changeType: 'positive' as const,
      color: 'text-purple-600',
      bgColor: 'bg-purple-50',
    }
  ];

  return (
    <ErrorBoundary>
      <div className="space-y-8">
        {/* Header */}
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Test Page</h1>
          <p className="text-muted-foreground">
            A comprehensive test page showcasing UI components, states, and interactions.
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-wrap gap-4">
          <Button onClick={handleToggleLoading} variant="outline">
            {isLoading ? <RefreshCw className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
            Toggle Loading State
          </Button>
          <Button onClick={handleToggleEmpty} variant="outline">
            <Eye className="mr-2 h-4 w-4" />
            Toggle Empty State
          </Button>
          <Button onClick={handleToggleError} variant="outline">
            <AlertCircle className="mr-2 h-4 w-4" />
            Toggle Error State
          </Button>
          <Button variant="default">
            <Plus className="mr-2 h-4 w-4" />
            Primary Action
          </Button>
          <Button variant="secondary">
            <Download className="mr-2 h-4 w-4" />
            Secondary Action
          </Button>
        </div>

        {/* Stats Grid */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {stats.map((stat) => {
            const Icon = stat.icon;
            return (
              <Card key={stat.title} className="hover:shadow-lg transition-shadow">
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">{stat.title}</CardTitle>
                  <div className={`p-2 rounded-md ${stat.bgColor}`}>
                    <Icon className={`h-4 w-4 ${stat.color}`} />
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {isLoading ? (
                      <Skeleton className="h-8 w-16" />
                    ) : (
                      stat.value
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground mt-1">{stat.description}</p>
                  <div className="flex items-center text-xs text-muted-foreground mt-2">
                    {stat.changeType === 'positive' ? (
                      <TrendingUp className="mr-1 h-3 w-3 text-green-500" />
                    ) : (
                      <Clock className="mr-1 h-3 w-3" />
                    )}
                    <span className={stat.changeType === 'positive' ? 'text-green-600' : 'text-red-600'}>
                      {stat.change}
                    </span>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {/* Tabs Component */}
        <Tabs defaultValue="components" className="w-full">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="components">Components</TabsTrigger>
            <TabsTrigger value="states">States</TabsTrigger>
            <TabsTrigger value="data">Data</TabsTrigger>
            <TabsTrigger value="interactions">Interactions</TabsTrigger>
          </TabsList>

          <TabsContent value="components" className="mt-6">
            <div className="grid gap-6 md:grid-cols-2">
              {/* Badges Showcase */}
              <Card>
                <CardHeader>
                  <CardTitle>Badge Components</CardTitle>
                  <CardDescription>Different badge variants and states</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex flex-wrap gap-2">
                    <Badge variant="default">Default</Badge>
                    <Badge variant="secondary">Secondary</Badge>
                    <Badge variant="outline">Outline</Badge>
                    <Badge variant="destructive">Destructive</Badge>
                  </div>
                  <Separator />
                  <div className="flex flex-wrap gap-2">
                    <Badge variant="default">
                      <CheckCircle className="w-3 h-3 mr-1" />
                      Active
                    </Badge>
                    <Badge variant="secondary">
                      <Clock className="w-3 h-3 mr-1" />
                      Pending
                    </Badge>
                    <Badge variant="destructive">
                      <AlertCircle className="w-3 h-3 mr-1" />
                      Error
                    </Badge>
                  </div>
                </CardContent>
              </Card>

              {/* Button Showcase */}
              <Card>
                <CardHeader>
                  <CardTitle>Button Components</CardTitle>
                  <CardDescription>Different button variants and sizes</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex flex-wrap gap-2">
                    <Button variant="default" size="sm">Small</Button>
                    <Button variant="default">Default</Button>
                    <Button variant="default" size="lg">Large</Button>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline">
                      <Settings className="mr-2 h-4 w-4" />
                      With Icon
                    </Button>
                    <Button variant="ghost">
                      <Upload className="mr-2 h-4 w-4" />
                      Ghost
                    </Button>
                    <Button disabled>
                      Disabled
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          <TabsContent value="states" className="mt-6">
            <div className="grid gap-6">
              <Card>
                <CardHeader>
                  <CardTitle>State Demonstrations</CardTitle>
                  <CardDescription>Loading, empty, and error states</CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  {isLoading && <LoadingState type="table" />}
                  
                  {showEmpty && (
                    <EmptyState
                      icon={<Users className="h-8 w-8 text-muted-foreground" />}
                      title="No test data available"
                      description="This is an example empty state component"
                    />
                  )}

                  {showError && (
                    <div className="text-center py-6">
                      <AlertCircle className="mx-auto h-12 w-12 text-red-500 mb-4" />
                      <p className="text-sm font-medium text-red-600">Something went wrong</p>
                      <p className="text-xs text-muted-foreground mt-1">This is a sample error state</p>
                    </div>
                  )}

                  {!isLoading && !showEmpty && !showError && (
                    <div className="text-center py-6">
                      <Info className="mx-auto h-12 w-12 text-blue-500 mb-4" />
                      <p className="text-sm font-medium">Normal State</p>
                      <p className="text-xs text-muted-foreground mt-1">Use the buttons above to test different states</p>
                    </div>
                  )}
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          <TabsContent value="data" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Sample Data Table</CardTitle>
                <CardDescription>Mock data display with actions</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {mockData.map((item) => (
                    <div key={item.id} className="flex items-center justify-between p-4 border rounded-lg">
                      <div className="flex items-center space-x-4">
                        <div className={`p-2 rounded-md ${
                          item.status === 'active' ? 'bg-green-50' :
                          item.status === 'warning' ? 'bg-yellow-50' : 'bg-red-50'
                        }`}>
                          <Zap className={`h-4 w-4 ${
                            item.status === 'active' ? 'text-green-600' :
                            item.status === 'warning' ? 'text-yellow-600' : 'text-red-600'
                          }`} />
                        </div>
                        <div>
                          <p className="font-medium">{item.name}</p>
                          <p className="text-sm text-muted-foreground">
                            {item.method} {item.path}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center space-x-2">
                        <Badge variant={
                          item.status === 'active' ? 'default' :
                          item.status === 'warning' ? 'secondary' : 'destructive'
                        }>
                          {item.status}
                        </Badge>
                        <span className="text-xs text-muted-foreground">{item.lastTested}</span>
                        <div className="flex space-x-1">
                          <Button variant="ghost" size="sm">
                            <Eye className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm">
                            <Edit3 className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm">
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="interactions" className="mt-6">
            <div className="grid gap-6 md:grid-cols-2">
              <Card>
                <CardHeader>
                  <CardTitle>Interactive Elements</CardTitle>
                  <CardDescription>Hover and click interactions</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <p className="text-sm font-medium">Hover Cards:</p>
                    <div className="grid grid-cols-2 gap-2">
                      <div className="p-4 border rounded-lg hover:shadow-md transition-shadow cursor-pointer">
                        <p className="text-sm">Hover me!</p>
                      </div>
                      <div className="p-4 border rounded-lg hover:bg-muted transition-colors cursor-pointer">
                        <p className="text-sm">Click me!</p>
                      </div>
                    </div>
                  </div>
                  <Separator />
                  <div className="space-y-2">
                    <p className="text-sm font-medium">Skeleton Loading:</p>
                    <div className="space-y-2">
                      <Skeleton className="h-4 w-full" />
                      <Skeleton className="h-4 w-3/4" />
                      <Skeleton className="h-4 w-1/2" />
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>System Information</CardTitle>
                  <CardDescription>Application metadata</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1">
                      <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                        Environment
                      </p>
                      <p className="text-sm font-semibold">Development</p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                        Version
                      </p>
                      <p className="text-sm font-semibold">1.0.0</p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                        Last Updated
                      </p>
                      <p className="text-sm font-semibold">{new Date().toLocaleString()}</p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                        Status
                      </p>
                      <Badge variant="default">
                        <div className="w-2 h-2 rounded-full bg-green-500 mr-1.5" />
                        Operational
                      </Badge>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </ErrorBoundary>
  );
}