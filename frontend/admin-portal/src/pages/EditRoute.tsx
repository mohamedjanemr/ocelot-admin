import { useState, useEffect } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { LoadingState } from '@/components/ui/loading-state';
import { ErrorBoundary } from '@/components/ui/error-boundary';
import { useApi, useMutation } from '../hooks/useApi';
import { routeConfigApi } from '../services/api';
import type { UpdateRouteConfig, HostAndPort } from '../types';
import toast from 'react-hot-toast';

export default function EditRoute() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: route, loading, error } = useApi(() => routeConfigApi.getById(id!), [id]);
  const updateRouteMutation = useMutation((data: UpdateRouteConfig) => routeConfigApi.update(id!, data));

  const [formData, setFormData] = useState<UpdateRouteConfig>({
    id: '',
    name: '',
    downstreamPathTemplate: '',
    upstreamPathTemplate: '',
    upstreamHttpMethod: 'GET',
    downstreamScheme: 'http',
    downstreamHostAndPorts: [{ host: 'localhost', port: 3000 }],
    environment: 'Development',
    serviceName: '',
  });

  // Update form data when route loads
  useEffect(() => {
    if (route) {
      setFormData({
        id: route.id,
        name: route.name,
        downstreamPathTemplate: route.downstreamPathTemplate,
        upstreamPathTemplate: route.upstreamPathTemplate,
        upstreamHttpMethod: route.upstreamHttpMethod,
        downstreamScheme: route.downstreamScheme,
        downstreamHostAndPorts: route.downstreamHostAndPorts || [{ host: 'localhost', port: 3000 }],
        environment: route.environment,
        serviceName: route.serviceName || '',
        loadBalancerOptions: route.loadBalancerOptions || '',
        authenticationOptions: route.authenticationOptions || '',
        rateLimitOptions: route.rateLimitOptions || '',
        qoSOptions: route.qoSOptions || '',
      });
    }
  }, [route]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      await updateRouteMutation.mutate(formData);
      toast.success('Route updated successfully!');
      navigate(`/routes/${id}`);
    } catch (error) {
      toast.error('Failed to update route');
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleHostPortChange = (index: number, field: keyof HostAndPort, value: string | number) => {
    setFormData(prev => ({
      ...prev,
      downstreamHostAndPorts: prev.downstreamHostAndPorts.map((item, i) =>
        i === index ? { ...item, [field]: field === 'port' ? Number(value) : value } : item
      )
    }));
  };

  const addHostPort = () => {
    setFormData(prev => ({
      ...prev,
      downstreamHostAndPorts: [...prev.downstreamHostAndPorts, { host: 'localhost', port: 3000 }]
    }));
  };

  const removeHostPort = (index: number) => {
    setFormData(prev => ({
      ...prev,
      downstreamHostAndPorts: prev.downstreamHostAndPorts.filter((_, i) => i !== index)
    }));
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
              <h1 className="text-3xl font-bold tracking-tight">Edit Route</h1>
              <p className="text-muted-foreground">Loading route information...</p>
            </div>
          </div>
          <LoadingState type="page" />
        </div>
      </ErrorBoundary>
    );
  }

  // Handle missing ID parameter
  if (!id) {
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
              <h1 className="text-3xl font-bold tracking-tight">Invalid Route</h1>
              <p className="text-muted-foreground">No route ID provided in the URL.</p>
            </div>
          </div>
        </div>
      </ErrorBoundary>
    );
  }

  if (error || (!loading && !route)) {
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
              <p className="text-muted-foreground">
                {error ? `Error: ${error}` : 'The requested route could not be found.'}
              </p>
            </div>
          </div>
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
              <Link to={`/routes/${id}`}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Route
              </Link>
            </Button>
            <div>
              <h1 className="text-3xl font-bold tracking-tight">Edit Route</h1>
              <p className="text-muted-foreground">
                Modify the configuration for "{route.name}"
              </p>
            </div>
          </div>
          <div className="flex items-center space-x-2">
            <Button variant="outline" onClick={() => navigate(`/routes/${id}`)}>
              <X className="mr-2 h-4 w-4" />
              Cancel
            </Button>
            <Button 
              form="route-form" 
              type="submit" 
              disabled={updateRouteMutation.loading}
            >
              <Save className="mr-2 h-4 w-4" />
              {updateRouteMutation.loading ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </div>

        {/* Form */}
        <form id="route-form" onSubmit={handleSubmit} className="space-y-8">
          <div className="grid gap-8 lg:grid-cols-2">
            {/* Basic Information */}
            <Card>
              <CardHeader>
                <CardTitle>Basic Information</CardTitle>
                <CardDescription>
                  Configure the basic route information and identification.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="space-y-2">
                  <label htmlFor="name" className="text-sm font-medium">
                    Route Name *
                  </label>
                  <input
                    id="name"
                    name="name"
                    type="text"
                    required
                    value={formData.name}
                    onChange={handleInputChange}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                    placeholder="e.g., Users API"
                  />
                </div>

                <div className="space-y-2">
                  <label htmlFor="environment" className="text-sm font-medium">
                    Environment *
                  </label>
                  <select
                    id="environment"
                    name="environment"
                    required
                    value={formData.environment}
                    onChange={handleInputChange}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  >
                    <option value="Development">Development</option>
                    <option value="Staging">Staging</option>
                    <option value="Production">Production</option>
                  </select>
                </div>

                <div className="space-y-2">
                  <label htmlFor="serviceName" className="text-sm font-medium">
                    Service Name
                  </label>
                  <input
                    id="serviceName"
                    name="serviceName"
                    type="text"
                    value={formData.serviceName}
                    onChange={handleInputChange}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                    placeholder="e.g., users-service"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Upstream Configuration */}
            <Card>
              <CardHeader>
                <CardTitle>Upstream Configuration</CardTitle>
                <CardDescription>
                  Configure how clients will access this route.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="space-y-2">
                  <label htmlFor="upstreamPathTemplate" className="text-sm font-medium">
                    Upstream Path Template *
                  </label>
                  <input
                    id="upstreamPathTemplate"
                    name="upstreamPathTemplate"
                    type="text"
                    required
                    value={formData.upstreamPathTemplate}
                    onChange={handleInputChange}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                    placeholder="e.g., /api/users/{everything}"
                  />
                </div>

                <div className="space-y-2">
                  <label htmlFor="upstreamHttpMethod" className="text-sm font-medium">
                    HTTP Method *
                  </label>
                  <select
                    id="upstreamHttpMethod"
                    name="upstreamHttpMethod"
                    required
                    value={formData.upstreamHttpMethod}
                    onChange={handleInputChange}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  >
                    <option value="GET">GET</option>
                    <option value="POST">POST</option>
                    <option value="PUT">PUT</option>
                    <option value="DELETE">DELETE</option>
                    <option value="PATCH">PATCH</option>
                    <option value="HEAD">HEAD</option>
                    <option value="OPTIONS">OPTIONS</option>
                  </select>
                </div>
              </CardContent>
            </Card>

            {/* Downstream Configuration */}
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Downstream Configuration</CardTitle>
                <CardDescription>
                  Configure how the gateway will forward requests to your backend service.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="grid gap-6 md:grid-cols-2">
                  <div className="space-y-2">
                    <label htmlFor="downstreamPathTemplate" className="text-sm font-medium">
                      Downstream Path Template *
                    </label>
                    <input
                      id="downstreamPathTemplate"
                      name="downstreamPathTemplate"
                      type="text"
                      required
                      value={formData.downstreamPathTemplate}
                      onChange={handleInputChange}
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                      placeholder="e.g., /users/{everything}"
                    />
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="downstreamScheme" className="text-sm font-medium">
                      Downstream Scheme *
                    </label>
                    <select
                      id="downstreamScheme"
                      name="downstreamScheme"
                      required
                      value={formData.downstreamScheme}
                      onChange={handleInputChange}
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                    >
                      <option value="http">HTTP</option>
                      <option value="https">HTTPS</option>
                    </select>
                  </div>
                </div>

                {/* Host and Ports */}
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <label className="text-sm font-medium">
                      Downstream Hosts & Ports *
                    </label>
                    <Button type="button" variant="outline" size="sm" onClick={addHostPort}>
                      Add Host
                    </Button>
                  </div>
                  
                  <div className="space-y-3">
                    {formData.downstreamHostAndPorts.map((hostPort, index) => (
                      <div key={index} className="flex items-center space-x-3">
                        <input
                          type="text"
                          value={hostPort.host}
                          onChange={(e) => handleHostPortChange(index, 'host', e.target.value)}
                          className="flex-1 h-10 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                          placeholder="Host (e.g., localhost)"
                        />
                        <input
                          type="number"
                          value={hostPort.port}
                          onChange={(e) => handleHostPortChange(index, 'port', e.target.value)}
                          className="w-24 h-10 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                          placeholder="Port"
                          min="1"
                          max="65535"
                        />
                        {formData.downstreamHostAndPorts.length > 1 && (
                          <Button 
                            type="button" 
                            variant="outline" 
                            size="sm"
                            onClick={() => removeHostPort(index)}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Advanced Configuration */}
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Advanced Configuration</CardTitle>
                <CardDescription>
                  Optional advanced configuration settings.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="grid gap-6 md:grid-cols-2">
                  <div className="space-y-2">
                    <label htmlFor="loadBalancerOptions" className="text-sm font-medium">
                      Load Balancer Options
                    </label>
                    <input
                      id="loadBalancerOptions"
                      name="loadBalancerOptions"
                      type="text"
                      value={formData.loadBalancerOptions || ''}
                      onChange={handleInputChange}
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                      placeholder="e.g., RoundRobin"
                    />
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="authenticationOptions" className="text-sm font-medium">
                      Authentication Options
                    </label>
                    <input
                      id="authenticationOptions"
                      name="authenticationOptions"
                      type="text"
                      value={formData.authenticationOptions || ''}
                      onChange={handleInputChange}
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                      placeholder="Authentication configuration"
                    />
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="rateLimitOptions" className="text-sm font-medium">
                      Rate Limit Options
                    </label>
                    <input
                      id="rateLimitOptions"
                      name="rateLimitOptions"
                      type="text"
                      value={formData.rateLimitOptions || ''}
                      onChange={handleInputChange}
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                      placeholder="Rate limiting configuration"
                    />
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="qoSOptions" className="text-sm font-medium">
                      Quality of Service Options
                    </label>
                    <input
                      id="qoSOptions"
                      name="qoSOptions"
                      type="text"
                      value={formData.qoSOptions || ''}
                      onChange={handleInputChange}
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                      placeholder="QoS configuration"
                    />
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </form>
      </div>
    </ErrorBoundary>
  );
} 