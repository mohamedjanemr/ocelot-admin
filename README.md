# Ocelot API Gateway with Admin Portal

A dynamic configuration management system for Ocelot API Gateway with a web-based admin portal.

## Project Overview

This project provides a comprehensive solution for managing Ocelot API Gateway configurations through a web-based admin portal. It eliminates the need for manual JSON file updates and redeployments by providing a dynamic configuration management system.

## Architecture

The project follows Clean Architecture principles with the following layers:

### Backend Components

1. **Domain Layer**: Core business entities and domain logic
   - Entities: RouteConfig, ConfigurationVersion
   - Value Objects: HostAndPort
   - Interfaces: IRouteConfigRepository, IConfigurationVersionRepository

2. **Application Layer**: Use cases and application services
   - Services for managing route configurations and configuration versions

3. **Infrastructure Layer**: Data access, external services
   - Entity Framework Core for database access
   - Repository implementations

4. **WebApi Layer**: FastEndpoints REST API
   - Endpoints for managing configurations
   - SignalR hubs for real-time updates

5. **Gateway Layer**: Ocelot API Gateway with dynamic config
   - Custom configuration provider for loading from database

### Frontend Components

1. **Admin Portal**: React + TypeScript web application
   - Vite for fast development and building
   - Tailwind CSS for styling
   - HeadlessUI for accessible components

2. **Real-time Updates**: SignalR integration for live config changes

## Technology Stack

- **Backend**: .NET 8, FastEndpoints, Ocelot, Entity Framework Core, SignalR
- **Frontend**: React, TypeScript, Vite, Tailwind CSS, HeadlessUI
- **Database**: SQL Server (via Entity Framework Core)

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js and npm
- SQL Server (or SQL Server Express)

### Setup

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/ocelot.ai.git
   cd ocelot.ai
   ```

2. Set up the backend:
   ```
   cd backend
   dotnet restore
   dotnet build
   ```

3. Set up the database:
   ```
   cd OcelotGateway.WebApi
   dotnet ef database update
   ```

4. Set up the frontend:
   ```
   cd ../../frontend/admin-portal
   npm install
   ```

### Running the Application

1. Start the backend:
   ```
   cd backend/OcelotGateway.WebApi
   dotnet run
   ```

2. Start the frontend:
   ```
   cd frontend/admin-portal
   npm run dev
   ```

3. Access the admin portal at `http://localhost:5173`

## Features

- Dynamic route management
- Service discovery configuration
- Load balancing configuration
- JWT authentication configuration
- Real-time monitoring of configuration changes
- Configuration versioning and rollback
- Multi-environment support

## Project Structure

```
ocelot.ai/
├── backend/
│   ├── OcelotGateway.Domain/
│   ├── OcelotGateway.Application/
│   ├── OcelotGateway.Infrastructure/
│   ├── OcelotGateway.WebApi/
│   └── OcelotGateway.Gateway/
├── frontend/
│   └── admin-portal/
└── memory-bank/
```

## License

This project is licensed under the MIT License - see the LICENSE file for details. 