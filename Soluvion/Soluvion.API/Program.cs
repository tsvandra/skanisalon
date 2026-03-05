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
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", 
                               "https://skanisalon.sk", 
                               "https://www.skanisalon.sk", 
                               "https://skanisalon-production.netlify.app",
                               "https://soluvion.netlify.app",
                               "https://develop--soluvion.netlify.app")  // Bárhonnan jöhet kérés (Netlify, localhost)
                  .AllowAnyMethod()  // GET, POST, PUT, DELETE, OPTIONS
                  .WithHeaders("X-Tenant-ID", "Authorization", "Content-Type"); // Bármilyen fejléc mehet
        });
});


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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Hiba az adatbázis migráció során: {ex.Message}");
    }
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");

app.UseMiddleware<TenantResolutionMiddleware>();

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

// Hitelesítés
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
