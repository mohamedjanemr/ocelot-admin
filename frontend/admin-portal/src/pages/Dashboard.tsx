import {
  Activity,
  ArrowUpRight,
  ChevronRight,
  Settings,
  Shield,
  Zap,
  TrendingUp,
  Clock,
  CheckCircle,
  AlertCircle,
} from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';
import { useApi } from '../hooks/useApi';
import { healthApi, routeConfigApi } from '../services/api';

export default function Dashboard() {
  const { data: healthData, loading: healthLoading, error: healthError } = useApi(() => healthApi.getHealth());
  const { data: routesData, loading: routesLoading } = useApi(() => routeConfigApi.getAll(1, 5));

  const stats = [
    {
      title: 'Total Routes',
      value: healthData?.totalRouteConfigs || 0,
      description: 'Active configurations',
      icon: Settings,
      change: '+4.75%',
      changeType: 'positive' as const,
      color: 'text-blue-600',
      bgColor: 'bg-blue-50',
    },
    {
      title: 'Active Services',
      value: healthData?.activeRouteConfigs || 0,
      description: 'Currently running',
      icon: CheckCircle,
      change: '+2.02%',
      changeType: 'positive' as const,
      color: 'text-green-600',
      bgColor: 'bg-green-50',
    },
    {
      title: 'Config Versions',
      value: healthData?.totalConfigurationVersions || 0,
      description: 'Stored versions',
      icon: Activity,
      change: '+19.03%',
      changeType: 'positive' as const,
      color: 'text-purple-600',
      bgColor: 'bg-purple-50',
    },
    {
      title: 'System Status',
      value: healthData?.status || 'Unknown',
      description: 'Overall health',
      icon: healthData?.status === 'Healthy' ? Shield : AlertCircle,
      change: healthData?.timestamp ? new Date(healthData.timestamp).toLocaleTimeString() : 'N/A',
      changeType: healthData?.status === 'Healthy' ? 'positive' : 'negative' as const,
      color: healthData?.status === 'Healthy' ? 'text-green-600' : 'text-red-600',
      bgColor: healthData?.status === 'Healthy' ? 'bg-green-50' : 'bg-red-50',
    },
  ];

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          Monitor your Ocelot Gateway configuration and system performance.
        </p>
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
                  {healthLoading ? (
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
                  <span className={stat.changeType === 'positive' ? 'text-green-600' : 'text-muted-foreground'}>
                    {stat.change}
                  </span>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Content Grid */}
      <div className="grid gap-6 md:grid-cols-2">
        {/* System Health */}
        <Card>
          <CardHeader>
            <div className="flex items-center space-x-2">
              <div className="p-2 bg-blue-50 rounded-md">
                <Activity className="h-4 w-4 text-blue-600" />
              </div>
              <div>
                <CardTitle>System Health</CardTitle>
                <CardDescription>Real-time system monitoring</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {healthLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-4 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
                <Skeleton className="h-4 w-2/3" />
              </div>
            ) : healthError ? (
              <div className="text-center py-6">
                <AlertCircle className="mx-auto h-12 w-12 text-red-500 mb-4" />
                <p className="text-sm font-medium text-red-600">Error loading health data</p>
                <p className="text-xs text-muted-foreground mt-1">{healthError}</p>
              </div>
            ) : (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">Overall Status</span>
                  <Badge variant={healthData?.status === 'Healthy' ? 'default' : 'destructive'}>
                    <div className="w-2 h-2 rounded-full bg-current mr-1.5" />
                    {healthData?.status}
                  </Badge>
                </div>
                <Separator />
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                      Version
                    </p>
                    <p className="text-sm font-semibold">{healthData?.version || 'N/A'}</p>
                  </div>
                  <div className="space-y-1">
                    <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                      Last Check
                    </p>
                    <p className="text-sm font-semibold">
                      {healthData?.timestamp ? new Date(healthData.timestamp).toLocaleTimeString() : 'N/A'}
                    </p>
                  </div>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Recent Activity */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <div className="p-2 bg-purple-50 rounded-md">
                  <Zap className="h-4 w-4 text-purple-600" />
                </div>
                <div>
                  <CardTitle>Recent Activity</CardTitle>
                  <CardDescription>Latest configuration changes</CardDescription>
                </div>
              </div>
              <ChevronRight className="h-4 w-4 text-muted-foreground" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {routesLoading ? (
                <div className="space-y-3">
                  {[1, 2, 3].map((i) => (
                    <div key={i} className="flex items-center space-x-3">
                      <Skeleton className="h-10 w-10 rounded-lg" />
                      <div className="space-y-1 flex-1">
                        <Skeleton className="h-4 w-3/4" />
                        <Skeleton className="h-3 w-1/2" />
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <>
                  {routesData?.routeConfigs?.slice(0, 3).map((route) => (
                    <div key={route.id} className="flex items-center space-x-3 p-3 rounded-lg bg-muted/50 hover:bg-muted transition-colors">
                      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50">
                        <Settings className="h-4 w-4 text-blue-600" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">
                          {route.upstreamPathTemplate}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {route.downstreamPathTemplate} • {route.downstreamScheme}
                        </p>
                      </div>
                      <div className="flex items-center space-x-2">
                        <Badge variant="secondary">Active</Badge>
                        <ArrowUpRight className="h-3 w-3 text-muted-foreground" />
                      </div>
                    </div>
                  )) || (
                    <div className="text-center py-8">
                      <Clock className="mx-auto h-8 w-8 text-muted-foreground mb-2" />
                      <p className="text-sm font-medium text-muted-foreground">No recent activity</p>
                      <p className="text-xs text-muted-foreground">Configuration changes will appear here</p>
                    </div>
                  )}
                </>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
} 