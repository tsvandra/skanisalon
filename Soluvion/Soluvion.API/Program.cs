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


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseNpgsql(connectionString));


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITranslationService, OpenAiTranslationService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "https://skanisalon.sk", "https://www.skanisalon.sk", "https://skanisalon-production.netlify.app")  // Bárhonnan jöhet kérés (Netlify, localhost)
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

// --- 2. MIDDLEWARE (SORREND A LÉNYEG!) ---

// 1. KÉZI HIBAKEZELÉS (Hogy lássuk, ha baj van, ne csak CORS hibát kapjunk)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Ha hiba van, akkor is próbáljuk meg válaszolni valamit, ne haljon meg némán
        Console.WriteLine($"KRITIKUS HIBA: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Szerver hiba történt. Nézd meg a Railway logokat!");
    }
});

// 2. KÉZI OPTIONS KEZELÉS (Preflight Fix)
// Ez "átveri" a böngészőt: ha OPTIONS kérés jön, azonnal rávágjuk, hogy "OK",
// még mielőtt a bonyolult logika elhasalhatna.
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Tenant-ID");
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        return; // Itt meg is állunk, nem engedjük tovább a hibás részhez
    }
    await next();
});


app.UseCors("AllowAll");

app.UseMiddleware<TenantResolutionMiddleware>();

AppContext.SetSwitch("Npqsql.EnableLegacyTimestampBehavior", true);

app.MapGet("/api/ping", () => "PONG! A szerver el es mukodik.");

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
