using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using miniApp.API.Auth;
using miniApp.API.Data;
using miniApp.API.Services;
using System;
using System.IO;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIG DATABASE ========== //
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// ========== CONFIG JWT AUTH ========== //
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_for_jwt_auth";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ========== ADD SERVICES & CONTROLLERS ========== //
builder.Services.AddScoped<JwtService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<QrService>();

// ========== SWAGGER CONFIG ========== //
if (builder.Configuration.GetValue<bool>("SwaggerEnabled"))
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "APP API", Version = "v1" });
        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Description = "Input your JWT token in this format: Bearer {your token here}",
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };
        c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { jwtSecurityScheme, Array.Empty<string>() }
        });
    });
}

// ========== CORS ========== //
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5052",
            "https://localhost:5056",
            "http://localhost:5067",
            "https://localhost:7079",
            "https://salereport.vibeandchic.com",
            "https://frontline.vibeandchic.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});


var app = builder.Build();

// ========== MIDDLEWARE ========== //
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                                       .CreateLogger("GlobalException");
        logger.LogError(ex, "Unhandled error");

        if (!ctx.Response.HasStarted)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { error = "Internal Server Error" });
        }
    }
});

app.UseStaticFiles();
if (builder.Configuration.GetValue<bool>("SwaggerEnabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

var imagesRoot = builder.Configuration["ImageRootPath"];
if (Directory.Exists(imagesRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imagesRoot),
        RequestPath = "/images"
    });
}

app.MapControllers();

// ===== Seed data =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DataSeeder.SeedAsync(context);
}


app.Run();
