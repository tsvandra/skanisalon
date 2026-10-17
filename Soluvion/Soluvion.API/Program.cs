using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Soluvion.API.Data;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Net;
using Soluvion.API.Services;
using Soluvion.API.Middleware;
using Microsoft.AspNetCore.Authentication;
using CloudinaryDotNet;
using Npgsql;
using Soluvion.API.Interfaces;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ITranslationService, OpenAiTranslationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ISmartBookingEngine, SmartBookingEngine>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICompanyAttributeService, CompanyAttributeService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDocumentTextExtractorService, DocumentTextExtractorService>();
builder.Services.AddScoped<IProductAiScannerService, ProductAiScannerService>();
builder.Services.AddHostedService<AppointmentAutoCompleteService>();

// Publikus foglalási végpont védelem (Rate Limiting spam és DoS ellen)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { errorCode = "TOO_MANY_BOOKING_REQUESTS" },
            cancellationToken);
    };

    options.AddPolicy("PublicBooking", httpContext =>
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var clientIp = !string.IsNullOrWhiteSpace(forwardedFor)
            ? forwardedFor.Split(',')[0].Trim()
            : (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.SetIsOriginAllowed(_ => true)  // Támogatja a tenantok egyedi domainjeit, aldomainjeit és a Netlify előnézeteket
                  .AllowAnyMethod()              // GET, POST, PUT, DELETE, OPTIONS
                  .AllowAnyHeader();             // X-Tenant-ID, X-Company-Id, X-Tenant-Domain, Authorization, Content-Type
        });
});


builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

var cloudinarySettings = builder.Configuration.GetSection("Cloudinary");
Account account = new Account(
    cloudinarySettings["CloudName"],
    cloudinarySettings["ApiKey"],
    cloudinarySettings["ApiSecret"]
);
Cloudinary cloudinary = new Cloudinary(account);
cloudinary.Api.Secure = true;
builder.Services.AddSingleton(cloudinary);

builder.Services.AddHttpContextAccessor();

// 3. OpenAPI Generálás (Ez gyártja le a doksit a Scalarnak)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

//Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false, // Fejlesztés alatt egyszerűsítünk
            ValidateAudience = false // Fejlesztés alatt egyszerűsítünk
        };
    });


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("Adatbázis migrációk ellenőrzése és futtatása...");
        db.Database.Migrate(); // Automatikusan lefuttatja a hiányzó migrációkat a Railway-en!
        Console.WriteLine("Adatbázis sikeresen frissítve!");

        Console.WriteLine("Alapértelmezett adatok ellenőrzése (Seeding)...");
        await DbSeeder.SeedAsync(db);
        Console.WriteLine("Seeding kész!");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Hiba az adatbázis migráció során: {ex.Message}");
    }
}

app.UseExceptionHandler();

app.UseCors("AllowAll");

app.UseRateLimiter();

AppContext.SetSwitch("Npqsql.EnableLegacyTimestampBehavior", true);

//app.MapGet("/api/ping", () => "PONG! A szerver el es mukodik.");

// Scalar Dokumentáció (Fejlesztői módban, vagy mindig)
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.UseStaticFiles();

// Hitelesítés (Authentication) a TenantResolution előtt fut le, hogy a middleware már lássa a JWT token Claimjeit!
app.UseAuthentication();

// Tenant feloldás (TenantResolutionMiddleware)
app.UseMiddleware<TenantResolutionMiddleware>();

// Jogosultságkezelés (Authorization)
app.UseAuthorization();

app.MapControllers();

app.Run();
