using EMS.API.Middleware;
using EMS.Application;
using EMS.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using EMS.API.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthRateLimit", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// Dynamic Authorization Policies
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EMS.Application.Interfaces.ICurrentUserService, EMS.API.Services.CurrentUserService>();

// 1. Setup Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// 2. Add services to the container.
builder.Services.AddControllers();

// 3. API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// 4. Setup Swagger with JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Employee Management System API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. JWT Authentication Setup
var jwtKey = builder.Configuration["Jwt:Key"] ?? "FallbackSecretKeyForJwtAuthenticationWhichIsVeryLongAndSecure12345!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 6. Layer DI
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 7. CORS setup
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteDevServer",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Seed Database automatically on startup
using (var scope = app.Services.CreateScope())
{
    await EMS.Infrastructure.Persistence.DataSeeder.SeedAsync(scope.ServiceProvider);
    
    // TEMPORARY: Update existing employees with NULL gender
    var dbContext = scope.ServiceProvider.GetRequiredService<EMS.Application.Interfaces.IApplicationDbContext>();
    var employeesWithoutGender = dbContext.Employees.Where(e => e.Gender == null).ToList();
    if (employeesWithoutGender.Any())
    {
        foreach (var emp in employeesWithoutGender)
        {
            var name = emp.FullName.ToLower();
            if (name.Contains("sri") || name.Contains("wati") || name.Contains("siti") || name.Contains("putri") || name.Contains("ayu") || name.Contains("ha"))
            {
                emp.Gender = EMS.Domain.Enums.Gender.Female;
            }
            else
            {
                emp.Gender = EMS.Domain.Enums.Gender.Male;
            }
        }
        await dbContext.SaveChangesAsync(default);
        
        // Also re-initialize leave balances so they get the gender-specific ones if applicable
        var leaveService = scope.ServiceProvider.GetRequiredService<EMS.Application.Leaves.ILeaveService>();
        await leaveService.InitializeLeaveBalancesAsync(DateTime.UtcNow.Year);
    }
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMS API v1"));
}

app.UseSerilogRequestLogging();

app.UseRateLimiter();

app.UseCors("AllowViteDevServer");

app.UseStaticFiles(); // Serve static files like uploaded photos

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
