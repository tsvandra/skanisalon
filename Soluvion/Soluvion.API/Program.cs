using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Soluvion.API.Data;
using Swashbuckle.AspNetCore.Filters;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseNpgsql(connectionString));

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll",
//        policy =>
//        {
//            policy.AllowAnyOrigin()  // Bárhonnan jöhet kérés (Netlify, localhost)
//                  .AllowAnyMethod()  // GET, POST, PUT, DELETE, OPTIONS
//                  .AllowAnyHeader(); // Bármilyen fejléc mehet
//        });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                    "https://soluvion.netlify.app", // A te frontend címed
                    "http://localhost:5173",        // Helyi fejlesztés
                    "http://localhost:3000"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Ez fontos lehet, ha cookie-t vagy auth headert küldesz!
        });
});


builder.Services.AddControllers();

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

AppContext.SetSwitch("Npqsql.EnableLegacyTimestampBehavior", true);

app.UseCors("AllowAll");
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



//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    try
//    {
//        var context = services.GetRequiredService<AppDbContext>();
//        if (context.Database.GetPendingMigrations().Any())
//        {
//            context.Database.Migrate();
//        }
//    }
//    catch (Exception ex)
//    {
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "Hiba történt az adatbázis migráció során.");
//    }
//}

app.UseStaticFiles();

// Hitelesítés
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
