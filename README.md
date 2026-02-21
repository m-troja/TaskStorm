# TaskStorm

**TaskStorm** is a high-performance, modular web application built with **ASP.NET Core** for managing tasks, projects, teams, and user activities. It integrates JWT authentication, PostgreSQL database, Serilog logging, and supports extensible REST APIs with **Swagger/OpenAPI** documentation.

---

## Version

- **.NET Version**: 10 (ASP.NET Core)
- **C# Language Version**: 12
- **Database**: PostgreSQL
- **Logging**: Serilog
- **Swagger/OpenAPI**: 6.6.0
- **JWT Authentication**: Microsoft.AspNetCore.Authentication.JwtBearer
- **ORM**: Entity Framework Core

---

## Features

- **Authentication & Authorization**
  - JWT token-based authentication
  - Role-based access control
  - Token regeneration endpoint (`POST /api/v1/auth/regenerate-tokens`)
  
- **User Management**
  - CRUD operations for users
  - Fetch users by ID or email
  - Delete all users (`DELETE /api/v1/user/all`)
  
- **Project Management**
  - Create, read, list all projects
  - Assign issues to projects
  - Project endpoints: `/api/v1/project/*`
  
- **Team Management**
  - Create teams, fetch team details, list users/issues by team
  - Team endpoints: `/api/v1/team/*`
  
- **Issue Tracking**
  - Create, assign, rename, update priority, status, due dates
  - Retrieve by ID, key, user, or project
  - Delete single or all issues
  - Issue endpoints: `/api/v1/issue/*`
  
- **Comments**
  - CRUD operations linked to issues
  - Comment endpoints: `/api/v1/comment/*`
  
- **Login & Registration**
  - User login (`POST /api/v1/login`)
  - User registration (`POST /api/v1/register`)
  
- **Testing & Debug**
  - Environment checks and profile endpoints
  - Test endpoints: `/api/v1/test/*`
  
- **ChatGPT Integration**
  - Custom service for AI-powered issue handling or task suggestions

---

## Technology Stack

| Component               | Details                                    |
|-------------------------|--------------------------------------------|
| **Language**            | C# 12                                      |
| **Framework**           | ASP.NET Core 10                          |
| **Database**            | PostgreSQL 16                              |
| **ORM**                 | Entity Framework Core 8                    |
| **Authentication**      | JWT (Microsoft.AspNetCore.Authentication) |
| **Logging**             | Serilog (File logging, rolling logs)      |
| **API Documentation**   | Swagger / OpenAPI 6.6.0                    |
| **JSON Serialization**  | System.Text.Json + Enums as Strings        |
| **Dependency Injection**| Built-in ASP.NET Core DI                    |
| **Unit testing**| XUnit                    |
---

## Environment Configuration

Create a `.env` or `dev.env` file in the root folder:

```bash
TS_LOG_DIR=/home/michal
TS_LOG_FILENAME=log
TS_HTTP_PORT=6901

JWT_SECRET=your_jwt_secret
JWT_ISSUER=TaskStorm
JWT_AUDIENCE=TaskStorm-Users
ACCESS_TOKEN_EXPIRY_MINUTES=2

DB_HOST=localhost
DB_NAME=TaskStorm
DB_PORT=5432
DB_USERNAME=task_user
DB_PASSWORD=StrongPassword123
```
Log files are automatically rolled daily and separated by debug, info, and error levels.

