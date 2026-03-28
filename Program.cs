// =============================================================================
// Program.cs — The DI composition root and middleware pipeline for .NET 8.
//
// Order of middleware matters in ASP.NET Core:
//   1. Exception handler (must be first to catch everything below it)
//   2. HTTPS redirection
//   3. CORS (must be before Auth)
//   4. Authentication (validates the JWT)
//   5. Authorization (checks the role claims)
//   6. Controller routing
// =============================================================================

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartClinic.API.Data;
using SmartClinic.API.Middleware;
using SmartClinic.API.Services;
using SmartClinic.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// SERVICE REGISTRATIONS (the DI container)
// =============================================================================

// --- Database ---
// Npgsql reads the Neon connection string and configures EF Core for Postgres
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            // Retry on transient failures (network hiccups with Neon serverless)
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

// --- Authentication: JWT Bearer ---
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is required.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        // Reject tokens within 5 seconds of expiry to handle clock skew
        ClockSkew = TimeSpan.FromSeconds(5),
    };
});

// --- Authorization ---
builder.Services.AddAuthorization();

// --- CORS — allow React dev server during development ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",  // Vite default dev port
                "http://localhost:3000"   // CRA fallback
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- Application Services (N-Tier: Controller → Service → DbContext) ---
// AddScoped = one instance per HTTP request (correct for EF Core DbContext pattern)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// --- API Infrastructure ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Swagger with JWT support ---
// Allows you to click "Authorize" in Swagger UI and paste your JWT token
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartClinic API",
        Version = "v1",
        Description = "Secure telehealth scheduling API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// =============================================================================
// BUILD & MIDDLEWARE PIPELINE
// =============================================================================
var app = builder.Build();

// --- Apply pending EF Core migrations automatically on startup ---
// In production you'd run `dotnet ef database update` in your CI/CD pipeline.
// For development, auto-migrate saves a manual step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware pipeline — ORDER IS CRITICAL
app.UseMiddleware<GlobalExceptionMiddleware>(); // 1. Catch all exceptions first

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartClinic API v1"));
}

app.UseHttpsRedirection();  // 2. Force HTTPS
app.UseCors("ReactFrontend"); // 3. CORS headers before auth
app.UseAuthentication();      // 4. Validate the JWT Bearer token
app.UseAuthorization();       // 5. Check [Authorize] role claims

app.MapControllers();         // 6. Route to controllers

app.Run();