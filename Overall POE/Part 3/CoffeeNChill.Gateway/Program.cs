using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Middleware;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

string jwtKey = builder.Configuration["Jwt:Key"] ?? "";

// HS256 needs a key of at least 32 bytes. Rather stop here than run with a weak key.
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be set and be at least 32 characters.");
}

// Controllers, with validation errors in the same format as all the other errors
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return new BadRequestObjectResult(new ErrorResponseDto
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = string.Join(" ", errors)
            });
        };
    });

// JWT authentication
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Our own services
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddHttpClient<FunctionsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Functions:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("x-functions-key", builder.Configuration["Functions:Key"]);
});

// Swagger with the XML comments and a button to enter the JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoffeeNChill API Gateway",
        Version = "v1",
        Description = "Log in with /api/auth/login, then click Authorize and paste the token."
    });

    string xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste only the token, without the word Bearer."
    });

    options.OperationFilter<SwaggerAuthFilter>();
});

var app = builder.Build();

// The Users table must exist, and the first Admin is created from configuration
// because only an Admin is allowed to create other Admin or Staff users.
var users = app.Services.GetRequiredService<UserService>();
await users.CreateTableAsync();

string? adminEmail = app.Configuration["AdminUser:Email"];
string? adminPassword = app.Configuration["AdminUser:Password"];

if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
{
    await users.CreateUserAsync("Canteen Manager", adminEmail, adminPassword, "Admin");
}

// The exception middleware is first so it catches errors from everything after it
app.UseMiddleware<ExceptionMiddleware>();

// Swagger stays on in production because the POE needs it on the live VPS
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
