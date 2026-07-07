# Employee Management System (EMS)

A comprehensive, enterprise-ready Employee Management System implementing complete RBAC, HR processes (Attendance, Leave), and more using a clean architecture approach.

## 🏛 Architecture & Tech Stack

This project strictly adheres to **Clean Architecture** principles to separate concerns into four logical layers:
- **Domain**: Core business entities and rules.
- **Application**: Business logic (Services) and use cases.
- **Infrastructure**: Data access (EF Core), integrations, external libraries.
- **API (Presentation)**: ASP.NET Core endpoints, routing, rate limiting.

### Tech Stack
- **Backend:** .NET 8, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, BCrypt, JWT, Serilog, ClosedXML.
- **Frontend:** React, TypeScript, Vite, React Router v6, Zustand, TanStack Query, Tailwind CSS, shadcn/ui.
- **Testing:** xUnit, Moq, FluentAssertions.
- **DevOps:** Docker, Docker Compose, GitHub Actions.

## 🚀 Running Locally (Without Docker)

1. **Database:** Make sure PostgreSQL is running on localhost:5432.
2. **Backend:**
   ```bash
   cd EMS.API
   dotnet ef database update --project ../EMS.Infrastructure/EMS.Infrastructure.csproj
   dotnet run
   ```
3. **Frontend:**
   ```bash
   cd ems-frontend
   npm install
   node node_modules/vite/bin/vite.js
   ```

## 🐳 Running with Docker

You can spin up the database, backend, and frontend with a single command:

```bash
docker-compose up --build
```
* Note: The database will be seeded automatically on startup.
- Backend API & Swagger: http://localhost:5000/swagger
- Frontend UI: http://localhost:3000

## 🔧 Environment Variables
The following can be configured (or are automatically injected in Docker):
- `ConnectionStrings__DefaultConnection`: Database connection string.
- `Jwt__Key`: Secret key for token signing.
- `Jwt__Issuer` & `Jwt__Audience`: Token validation settings.

## 🧠 Technical Decisions
- **Refresh Token Rotation:** Instead of keeping tokens valid forever, access tokens expire in 15 minutes. A persistent refresh token is used to seamlessly generate new access tokens.
- **Permission-based vs Role-based:** Hardcoding roles (`if(role=="Admin")`) breaks easily. We tie permissions to roles dynamically in DB. The UI and API check `employee.write` permissions, not generic roles.
- **Haversine Formula for Geofencing:** Instead of heavy GIS DB plugins (like PostGIS), calculating straight-line distance in C# ensures optimal performance for attendance clock-ins.
