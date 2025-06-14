// API Types matching backend DTOs

export interface HostAndPort {
  host: string;
  port: number;
}

export interface RouteConfig {
  id: string;
  name: string;
  downstreamPathTemplate: string;
  upstreamPathTemplate: string;
  upstreamHttpMethod: string;
  upstreamHttpMethods: string[];
  downstreamHttpMethod: string;
  downstreamScheme: string;
  routeIsCaseSensitive: boolean;
  downstreamHostAndPorts: HostAndPort[];
  serviceName?: string;
  loadBalancerOptions?: string;
  authenticationOptions?: string;
  rateLimitOptions?: string;
  qoSOptions?: string;
  isActive: boolean;
  environment: string;
  createdAt: string;
  updatedAt?: string;
  createdBy: string;
  updatedBy: string;
}

export interface CreateRouteConfig {
  name: string;
  downstreamPathTemplate: string;
  upstreamPathTemplate: string;
  upstreamHttpMethod: string;
  downstreamScheme: string;
  downstreamHostAndPorts: HostAndPort[];
  environment: string;
  serviceName?: string;
  loadBalancerOptions?: string;
  authenticationOptions?: string;
  rateLimitOptions?: string;
  qoSOptions?: string;
}

export interface UpdateRouteConfig extends CreateRouteConfig {
  id: string;
}

export interface ConfigurationVersion {
  id: string;
  version: string;
  description?: string;
  environment: string;
  isActive: boolean;
  createdAt: string;
  publishedAt?: string;
  createdBy: string;
  publishedBy: string;
  routeConfigurations: RouteConfig[];
}

export interface CreateConfigurationVersion {
  version: string;
  description?: string;
  environment: string;
  routeIds: string[];
}

// API Response Types
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface RouteConfigResponse extends PaginatedResponse<RouteConfig> {
  routeConfigs: RouteConfig[];
}

export interface ConfigurationVersionResponse extends PaginatedResponse<ConfigurationVersion> {
  configurationVersions: ConfigurationVersion[];
}

// Health Check Types
export interface HealthCheckResponse {
  status: string;
  timestamp: string;
  services: Record<string, unknown>;
  version: string;
  totalRouteConfigs: number;
  activeRouteConfigs: number;
  totalConfigurationVersions: number;
}

// Form Types
export type RouteFormData = CreateRouteConfig;

export type ConfigurationVersionFormData = CreateConfigurationVersion;

// SignalR Types
export interface SignalRNotification {
  type: 'RouteConfigCreated' | 'RouteConfigUpdated' | 'RouteConfigDeleted' | 'ConfigurationVersionActivated';
  data: Record<string, unknown>;
  timestamp: string;
}

// Environment Types
export type Environment = 'Development' | 'Staging' | 'Production';

// HTTP Methods
export const HTTP_METHODS = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'] as const;
export type HttpMethod = typeof HTTP_METHODS[number];

// Load Balancer Types
export const LOAD_BALANCER_TYPES = ['RoundRobin', 'LeastConnection', 'NoLoadBalancer'] as const;
export type LoadBalancerType = typeof LOAD_BALANCER_TYPES[number];

// Downstream Schemes
export const DOWNSTREAM_SCHEMES = ['http', 'https'] as const;
export type DownstreamScheme = typeof DOWNSTREAM_SCHEMES[number]; 