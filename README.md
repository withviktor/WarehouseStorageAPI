# Warehouse Storage API

A comprehensive .NET 9 Web API for managing warehouse inventory with integrated LED location indicators and user authentication.

## 🚀 Features

### Inventory Management
- **Full CRUD operations** for storage items
- **Advanced search and filtering** by name, SKU, location, and category
- **Real-time inventory tracking** with quantity management
- **Location-based organization** with LED zone mapping
- **Item categorization** and detailed descriptions

### LED Integration
- **Physical LED indicators** for item locations in the warehouse
- **Zone-based LED control** (1-100 zones supported)
- **Multiple LED actions**: on/off, blink, pulse
- **Customizable colors and brightness**
- **Admin-only LED control** for security

### Authentication & Authorization
- **JWT-based authentication** with secure token management
- **Role-based access control** (User/Admin roles)
- **User management** with admin privileges
- **Comprehensive audit logging** of all user actions
- **Session tracking** and login history

### API Features
- **RESTful API design** with OpenAPI/Swagger documentation
- **CORS support** for React Native and web applications
- **Comprehensive error handling** and validation
- **Development-friendly** with detailed Swagger UI

## 🛠️ Technology Stack

- **.NET 9** - Latest .NET framework
- **Entity Framework Core** - ORM with SQLite database
- **JWT Authentication** - Secure token-based auth
- **Swagger/OpenAPI** - API documentation
- **SQLite** - Lightweight database
- **Serial Port Communication** - For LED controller hardware

## 📋 Prerequisites

- .NET 9 SDK
- SQLite (included)
- LED controller hardware (optional for full functionality)

## 🚀 Quick Start

### 1. Clone and Setup
```bash
git clone <your-repository-url>
cd WarehouseStorageAPI
dotnet restore
```

### 2. Database Setup
The database will be automatically created and seeded on first run with:
- Sample storage items
- Default admin user (check the seeder files for credentials)

### 3. Configuration
Update `appsettings.json` or `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=warehouse.db"
  },
  "Jwt": {
    "Key": "your-super-secret-key-that-should-be-at-least-256-bits-long"
  }
}
```

### 4. Run the Application
```bash
dotnet run
```

The API will be available at:
- **Swagger UI**: https://localhost:5001 (or http://localhost:5000)
- **API Base**: https://localhost:5001/api

## 📚 API Endpoints

### Authentication (`/api/auth`)
- `POST /login` - User login
- `POST /users` - Create new user (Admin only)
- `GET /users` - List all users (Admin only)
- `GET /users/{id}` - Get user details (Admin only)
- `DELETE /users/{id}` - Deactivate user (Admin only)

### Storage Management (`/api/storage`)
- `GET /` - Get all items (with search/filter)
- `GET /{id}` - Get specific item
- `POST /` - Create new item
- `PUT /{id}` - Update item
- `DELETE /{id}` - Delete item
- `POST /{id}/locate` - Light up LED for item location

### LED Control (`/api/led`) - Admin Only
- `GET /status` - Check LED controller connection
- `POST /command` - Send LED command

## 🗄️ Database Schema

### StorageItem
- Basic item information (Name, SKU, Description)
- Inventory tracking (Quantity, Location)
- LED zone mapping
- Categorization and pricing
- Audit timestamps

### User
- Authentication credentials
- Role-based permissions
- Personal information
- Activity tracking

### UserAction
- Comprehensive audit log
- Action types and descriptions
- IP address tracking
- Timestamp records

## 🔐 Authentication

The API uses JWT tokens for authentication. Include the token in requests:
```
Authorization: Bearer <your-jwt-token>
```

### Roles
- **User**: Can view and manage inventory
- **Admin**: Full access including user management and LED control

## 🔍 Usage Examples

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "userId": "1002412",
  "password": "your-password"
}
```

### Search Items
```http
GET /api/storage?search=widget&category=electronics&activeOnly=true
Authorization: Bearer <token>
```

### Control LEDs
```http
POST /api/led/command
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "zone": 5,
  "color": "blue",
  "action": "blink",
  "duration": 10000,
  "brightness": 200
}
```

## 🔧 Development

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### Seeding Data
The application automatically seeds initial data on startup. Modify the files in the `Seeds/` directory to customize initial data.

## 🏗️ Project Structure

```
├── Controllers/          # API controllers
├── Data/                # Database context
├── DTOs/                # Data transfer objects
├── Models/              # Entity models
├── Services/            # Business logic services
├── Seeds/               # Database seeding
├── Migrations/          # EF Core migrations
└── Properties/          # Launch settings
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📝 License

This project is part of a homelab setup for warehouse management and LED location indication.

---

**Note**: This API is designed to work with physical LED hardware for warehouse location indication. The LED functionality requires compatible hardware connected via serial port.
