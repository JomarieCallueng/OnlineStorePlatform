using Storefront.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient<GatewayClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:8080");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapRazorPages();
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
