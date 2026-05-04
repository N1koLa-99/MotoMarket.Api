using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Middleware;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services;
using MotoMarket.Api.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================
// Configuration
// =========================
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.Configure<PaidActionsOptions>(
    builder.Configuration.GetSection(PaidActionsOptions.SectionName));

builder.Services.Configure<AzureBlobOptions>(
    builder.Configuration.GetSection(AzureBlobOptions.SectionName));

builder.Services.Configure<MyPosOptions>(
    builder.Configuration.GetSection(MyPosOptions.SectionName));

builder.Services.Configure<FixedExchangeRatesOptions>(
    builder.Configuration.GetSection(FixedExchangeRatesOptions.SectionName));

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.Configure<AccountCodeOptions>(
    builder.Configuration.GetSection(AccountCodeOptions.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("Missing Jwt configuration.");

// =========================
// Upload limits
// =========================
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

// =========================
// DI - Infrastructure
// =========================
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// =========================
// DI - Repositories
// =========================
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IListingRepository, ListingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAccountCodeRepository, AccountCodeRepository>();

// =========================
// DI - Services
// =========================
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IListingValidationService, ListingValidationService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IListingPresentationService, ListingPresentationService>();
builder.Services.AddScoped<IBlobImageService, BlobImageService>();
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
builder.Services.AddScoped<IMyPosSignatureService, MyPosSignatureService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// =========================
// MVC / Controllers
// =========================
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var fieldErrors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e =>
                            string.IsNullOrWhiteSpace(e.ErrorMessage)
                                ? "Невалидна стойност."
                                : e.ErrorMessage)
                        .ToArray());

            var response = new ApiErrorResponse
            {
                Error = "Невалидни входни данни.",
                Code = "validation_error",
                TraceId = context.HttpContext.TraceIdentifier,
                FieldErrors = fieldErrors
            };

            return new BadRequestObjectResult(response);
        };
    });

// =========================
// Swagger
// =========================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MotoMarket API",
        Version = "v1"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Постави JWT token така: Bearer {token}",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// =========================
// Authentication
// =========================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// =========================
// Authorization
// =========================
builder.Services.AddAuthorization();

// =========================
// CORS
// =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                // Local frontend - Live Server
                "http://127.0.0.1:5500",
                "http://localhost:5500",
                "http://127.0.0.1:5501",
                "http://localhost:5501",

                // Local frontend - React/Vite/etc.
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173",

                // Production frontend
                "https://motositemages.z6.web.core.windows.net",
                "https://moto-zona.com",
                "https://www.moto-zona.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// =========================
// Middleware
// =========================
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MotoMarket API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("Frontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();