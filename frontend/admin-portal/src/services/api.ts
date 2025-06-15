import axios, { type AxiosResponse } from 'axios';
import type {
  RouteConfig,
  CreateRouteConfig,
  UpdateRouteConfig,
  ConfigurationVersion,
  CreateConfigurationVersion,
  RouteConfigResponse,
  ConfigurationVersionResponse,
  HealthCheckResponse,
} from '../types';

// API Configuration
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 seconds timeout
});

// Request interceptor for logging
apiClient.interceptors.request.use(
  (config) => {
    console.log(`🚀 API Request: ${config.method?.toUpperCase()} ${config.url}`);
    return config;
  },
  (error) => {
    console.error('❌ API Request Error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor for logging
apiClient.interceptors.response.use(
  (response) => {
    console.log(`✅ API Response: ${response.status} ${response.config.url}`);
    return response;
  },
  (error) => {
    console.error('❌ API Response Error:', error.response?.status, error.response?.data);
    return Promise.reject(error);
  }
);

// Route Configuration API
export const routeConfigApi = {
  // Get all route configurations
  getAll: async (page: number = 1, pageSize: number = 10): Promise<{ items: RouteConfig[]; totalCount: number }> => {
    const response: AxiosResponse<RouteConfigResponse> = await apiClient.get(
      `/api/route-configs?page=${page}&pageSize=${pageSize}`
    );
    // Transform the response to match what usePaginatedApi expects
    return {
      items: response.data.routeConfigs || [],
      totalCount: response.data.totalCount || 0
    };
  },

  // Get route configuration by ID
  getById: async (id: string): Promise<RouteConfig> => {
    const response: AxiosResponse<{ routeConfig: RouteConfig }> = await apiClient.get(
      `/api/route-configs/${id}`
    );
    return response.data.routeConfig;
  },

  // Create new route configuration
  create: async (routeConfig: CreateRouteConfig): Promise<RouteConfig> => {
    const response: AxiosResponse<RouteConfig> = await apiClient.post(
      '/api/route-configs',
      routeConfig
    );
    return response.data;
  },

  // Update route configuration
  update: async (id: string, routeConfig: UpdateRouteConfig): Promise<RouteConfig> => {
    const response: AxiosResponse<{ routeConfig: RouteConfig }> = await apiClient.put(
      `/api/route-configs/${id}`,
      routeConfig
    );
    return response.data.routeConfig;
  },

  // Delete route configuration
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/route-configs/${id}`);
  },
};

// Configuration Version API
export const configurationVersionApi = {
  // Get all configuration versions
  getAll: async (page: number = 1, pageSize: number = 10): Promise<{ items: ConfigurationVersion[]; totalCount: number }> => {
    const response: AxiosResponse<ConfigurationVersionResponse> = await apiClient.get(
      `/api/configuration-versions?page=${page}&pageSize=${pageSize}`
    );
    // Transform the response to match what usePaginatedApi expects
    return {
      items: response.data.configurationVersions || [],
      totalCount: response.data.totalCount || 0
    };
  },

  // Get configuration version by ID
  getById: async (id: string): Promise<ConfigurationVersion> => {
    const response: AxiosResponse<{ configurationVersion: ConfigurationVersion }> = await apiClient.get(
      `/api/configuration-versions/${id}`
    );
    return response.data.configurationVersion;
  },

  // Create new configuration version
  create: async (version: CreateConfigurationVersion): Promise<ConfigurationVersion> => {
    const response: AxiosResponse<ConfigurationVersion> = await apiClient.post(
      '/api/configuration-versions',
      version
    );
    return response.data;
  },

  // Activate configuration version
  activate: async (id: string): Promise<ConfigurationVersion> => {
    const response: AxiosResponse<{ configurationVersion: ConfigurationVersion }> = await apiClient.post(
      `/api/configuration-versions/${id}/activate`
    );
    return response.data.configurationVersion;
  },
};

// Health Check API
export const healthApi = {
  // Get system health status
  getHealth: async (): Promise<HealthCheckResponse> => {
    const response: AxiosResponse<HealthCheckResponse> = await apiClient.get('/health');
    return response.data;
  },
};

// Generic API error handler
export const handleApiError = (error: unknown): string => {
  if (error && typeof error === 'object' && 'response' in error) {
    const axiosError = error as { response: { data?: { message?: string }; status: number; statusText: string } };
    return axiosError.response.data?.message || `HTTP Error ${axiosError.response.status}: ${axiosError.response.statusText}`;
  } else if (error && typeof error === 'object' && 'request' in error) {
    // Request was made but no response received
    return 'Network error: Unable to reach the server. Please check your connection.';
  } else if (error && typeof error === 'object' && 'message' in error) {
    // Something else happened
    return (error as { message: string }).message;
  } else {
    return 'An unexpected error occurred';
  }
};

// Export the configured axios instance for custom requests
export { apiClient };
export default apiClient; 