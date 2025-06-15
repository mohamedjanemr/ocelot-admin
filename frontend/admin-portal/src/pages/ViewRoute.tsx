import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Edit, Trash2, Copy, ExternalLink } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { LoadingState } from '@/components/ui/loading-state';
import { ErrorBoundary } from '@/components/ui/error-boundary';
import { useApi, useMutation } from '../hooks/useApi';
import { routeConfigApi } from '../services/api';
import toast from 'react-hot-toast';

export default function ViewRoute() {
  const { id } = useParams<{ id: string }>();
  const { data: route, loading, error, refetch } = useApi(() => routeConfigApi.getById(id!), [id]);
  const deleteRouteMutation = useMutation((routeId: string) => routeConfigApi.delete(routeId));

  const handleDelete = async () => {
    if (!route) return;
    
    if (window.confirm(`Are you sure you want to delete "${route.name}"? This action cannot be undone.`)) {
      try {
        await deleteRouteMutation.mutate(route.id);
        toast.success('Route deleted successfully!');
        // Navigate back to routes list
        window.location.href = '/routes';
      } catch (error) {
        toast.error('Failed to delete route');
      }
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast.success('Copied to clipboard!');
  };

  if (loading) {
    return (
      <ErrorBoundary>
        <div className="space-y-8">
          <div className="flex items-center space-x-4">
            <Button variant="outline" size="sm" asChild>
              <Link to="/routes">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Routes
              </Link>
            </Button>
            <div>
              <h1 className="text-3xl font-bold tracking-tight">Route Details</h1>
              <p className="text-muted-foreground">Loading route information...</p>
            </div>
          </div>
          <LoadingState type="page" />
        </div>
      </ErrorBoundary>
    );
  }

  if (error || !route) {
    return (
      <ErrorBoundary>
        <div className="space-y-8">
          <div className="flex items-center space-x-4">
            <Button variant="outline" size="sm" asChild>
              <Link to="/routes">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Routes
              </Link>
            </Button>
            <div>
              <h1 className="text-3xl font-bold tracking-tight">Route Not Found</h1>
              <p className="text-muted-foreground">The requested route could not be found.</p>
            </div>
          </div>
          <Card>
            <CardContent className="p-12 text-center">
              <p className="text-muted-foreground">{error || 'Route not found'}</p>
              <Button onClick={refetch} className="mt-4">
                Try Again
              </Button>
            </CardContent>
          </Card>
        </div>
      </ErrorBoundary>
    );
  }

  return (
    <ErrorBoundary>
      <div className="space-y-8">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <Button variant="outline" size="sm" asChild>
              <Link to="/routes">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Routes
              </Link>
            </Button>
            <div>
              <h1 className="text-3xl font-bold tracking-tight">{route.name}</h1>
              <p className="text-muted-foreground">Route configuration details</p>
            </div>
          </div>
          <div className="flex items-center space-x-2">
            <Button variant="outline" asChild>
              <Link to={`/routes/${route.id}/edit`}>
                <Edit className="mr-2 h-4 w-4" />
                Edit Route
              </Link>
            </Button>
            <Button 
              variant="destructive" 
              onClick={handleDelete}
              disabled={deleteRouteMutation.loading}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              {deleteRouteMutation.loading ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </div>

        <div className="grid gap-8 lg:grid-cols-2">
          {/* Basic Information */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                Basic Information
                <Badge variant={route.isActive ? "default" : "secondary"}>
                  {route.isActive ? 'Active' : 'Inactive'}
                </Badge>
              </CardTitle>
              <CardDescription>
                Route identification and basic settings
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid gap-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Route Name</label>
                  <div className="flex items-center justify-between mt-1">
                    <p className="text-sm font-mono">{route.name}</p>
                    <Button 
                      variant="ghost" 
                      size="sm"
                      onClick={() => copyToClipboard(route.name)}
                    >
                      <Copy className="h-4 w-4" />
                    </Button>
                  </div>
                </div>

                <div>
                  <label className="text-sm font-medium text-muted-foreground">Environment</label>
                  <div className="mt-1">
                    <Badge variant="outline">{route.environment}</Badge>
                  </div>
                </div>

                {route.serviceName && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Service Name</label>
                    <p className="text-sm mt-1">{route.serviceName}</p>
                  </div>
                )}

                <div>
                  <label className="text-sm font-medium text-muted-foreground">Route ID</label>
                  <div className="flex items-center justify-between mt-1">
                    <p className="text-xs font-mono text-muted-foreground">{route.id}</p>
                    <Button 
                      variant="ghost" 
                      size="sm"
                      onClick={() => copyToClipboard(route.id)}
                    >
                      <Copy className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Status & Metadata */}
          <Card>
            <CardHeader>
              <CardTitle>Status & Metadata</CardTitle>
              <CardDescription>
                Route status and timestamps
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid gap-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Created</label>
                  <p className="text-sm mt-1">
                    {new Date(route.createdAt).toLocaleString()}
                  </p>
                  <p className="text-xs text-muted-foreground">by {route.createdBy}</p>
                </div>

                {route.updatedAt && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Last Updated</label>
                    <p className="text-sm mt-1">
                      {new Date(route.updatedAt).toLocaleString()}
                    </p>
                    <p className="text-xs text-muted-foreground">by {route.updatedBy}</p>
                  </div>
                )}

                <div>
                  <label className="text-sm font-medium text-muted-foreground">Case Sensitive</label>
                  <p className="text-sm mt-1">
                    {route.routeIsCaseSensitive ? 'Yes' : 'No'}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Upstream Configuration */}
          <Card>
            <CardHeader>
              <CardTitle>Upstream Configuration</CardTitle>
              <CardDescription>
                How clients access this route
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div>
                <label className="text-sm font-medium text-muted-foreground">Path Template</label>
                <div className="flex items-center justify-between mt-1">
                  <p className="text-sm font-mono bg-muted px-2 py-1 rounded">
                    {route.upstreamPathTemplate}
                  </p>
                  <Button 
                    variant="ghost" 
                    size="sm"
                    onClick={() => copyToClipboard(route.upstreamPathTemplate)}
                  >
                    <Copy className="h-4 w-4" />
                  </Button>
                </div>
              </div>

              <div>
                <label className="text-sm font-medium text-muted-foreground">HTTP Method</label>
                <div className="mt-1">
                  <Badge variant="secondary">{route.upstreamHttpMethod}</Badge>
                </div>
              </div>

              {route.upstreamHttpMethods && route.upstreamHttpMethods.length > 0 && (
                <div>
                  <label className="text-sm font-medium text-muted-foreground">All HTTP Methods</label>
                  <div className="flex flex-wrap gap-1 mt-1">
                    {route.upstreamHttpMethods.map((method) => (
                      <Badge key={method} variant="outline" className="text-xs">
                        {method}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Downstream Configuration */}
          <Card>
            <CardHeader>
              <CardTitle>Downstream Configuration</CardTitle>
              <CardDescription>
                How requests are forwarded to backend services
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div>
                <label className="text-sm font-medium text-muted-foreground">Path Template</label>
                <div className="flex items-center justify-between mt-1">
                  <p className="text-sm font-mono bg-muted px-2 py-1 rounded">
                    {route.downstreamPathTemplate}
                  </p>
                  <Button 
                    variant="ghost" 
                    size="sm"
                    onClick={() => copyToClipboard(route.downstreamPathTemplate)}
                  >
                    <Copy className="h-4 w-4" />
                  </Button>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">HTTP Method</label>
                  <div className="mt-1">
                    <Badge variant="secondary">{route.downstreamHttpMethod}</Badge>
                  </div>
                </div>

                <div>
                  <label className="text-sm font-medium text-muted-foreground">Scheme</label>
                  <div className="mt-1">
                    <Badge variant="outline">{route.downstreamScheme}</Badge>
                  </div>
                </div>
              </div>

              <div>
                <label className="text-sm font-medium text-muted-foreground">Host & Ports</label>
                <div className="space-y-2 mt-1">
                  {route.downstreamHostAndPorts?.map((hostPort, index) => (
                    <div key={index} className="flex items-center justify-between p-2 bg-muted rounded">
                      <p className="text-sm font-mono">
                        {hostPort.host}:{hostPort.port}
                      </p>
                      <div className="flex items-center space-x-1">
                        <Button 
                          variant="ghost" 
                          size="sm"
                          onClick={() => copyToClipboard(`${hostPort.host}:${hostPort.port}`)}
                        >
                          <Copy className="h-4 w-4" />
                        </Button>
                        <Button 
                          variant="ghost" 
                          size="sm"
                          onClick={() => window.open(`${route.downstreamScheme}://${hostPort.host}:${hostPort.port}`, '_blank')}
                        >
                          <ExternalLink className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Additional Configuration (if any) */}
        {(route.loadBalancerOptions || route.authenticationOptions || route.rateLimitOptions || route.qoSOptions) && (
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle>Advanced Configuration</CardTitle>
              <CardDescription>
                Additional route configuration options
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid gap-6 md:grid-cols-2">
                {route.loadBalancerOptions && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Load Balancer</label>
                    <p className="text-sm mt-1 font-mono bg-muted px-2 py-1 rounded">
                      {route.loadBalancerOptions}
                    </p>
                  </div>
                )}

                {route.authenticationOptions && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Authentication</label>
                    <p className="text-sm mt-1 font-mono bg-muted px-2 py-1 rounded">
                      {route.authenticationOptions}
                    </p>
                  </div>
                )}

                {route.rateLimitOptions && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Rate Limiting</label>
                    <p className="text-sm mt-1 font-mono bg-muted px-2 py-1 rounded">
                      {route.rateLimitOptions}
                    </p>
                  </div>
                )}

                {route.qoSOptions && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Quality of Service</label>
                    <p className="text-sm mt-1 font-mono bg-muted px-2 py-1 rounded">
                      {route.qoSOptions}
                    </p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </ErrorBoundary>
  );
} 