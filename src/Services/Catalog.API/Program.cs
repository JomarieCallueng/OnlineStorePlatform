using Asp.Versioning;
using Catalog.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Store.Shared.Common.Extensions;
using Store.Shared.Common.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddStoreCorrelation();
builder.Services.AddStoreProblemDetails();
builder.Services.AddStoreJwtAuth(builder.Configuration);

builder.Services.AddDbContext<CatalogDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Paste the raw JWT (no 'Bearer ' prefix)."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

await CatalogDbInitializer.InitializeAsync(app);

app.UseStoreCorrelation();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1"));
}

app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
