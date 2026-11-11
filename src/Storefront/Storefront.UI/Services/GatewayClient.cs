using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Storefront.UI.Models;

namespace Storefront.UI.Services;

/// <summary>
/// The storefront's single door to the backend: every call goes through the API gateway.
/// Attaches the session JWT when present and turns Problem Details responses into GatewayException.
/// </summary>
public sealed class GatewayClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;

    public GatewayClient(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    // ---- auth --------------------------------------------------------------
    public Task<AuthResponse> LoginAsync(string username, string password) =>
        PostReadAsync<AuthResponse>("/auth/v1/login", new { username, password }, auth: false);

    public Task<AuthResponse> RegisterAsync(string username, string password) =>
        PostReadAsync<AuthResponse>("/auth/v1/register", new { username, password }, auth: false);

    // ---- catalog -----------------------------------------------------------
    public Task<List<ProductDto>> GetProductsAsync(bool includeInactive = false) =>
        GetAsync<List<ProductDto>>($"/catalog/v1/products?includeInactive={includeInactive.ToString().ToLowerInvariant()}", auth: false);

    public Task<ProductDto> CreateProductAsync(CreateProductRequest req) =>
        PostReadAsync<ProductDto>("/catalog/v1/products", req);

    public Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest req) =>
        SendReadAsync<ProductDto>(HttpMethod.Put, $"/catalog/v1/products/{id}", req);

    // ---- inventory ---------------------------------------------------------
    public Task<List<StockDto>> GetStockAsync() => GetAsync<List<StockDto>>("/inventory/v1/stock");

    public Task<StockDto> AdjustStockAsync(Guid productId, int quantityOnHand) =>
        SendReadAsync<StockDto>(HttpMethod.Put, $"/inventory/v1/stock/{productId}", new AdjustStockRequest(quantityOnHand));

    // ---- orders ------------------------------------------------------------
    public Task<OrderDto> PlaceOrderAsync(PlaceOrderRequest req) =>
        PostReadAsync<OrderDto>("/order/v1/orders", req);

    public Task<List<OrderDto>> GetOrdersAsync() => GetAsync<List<OrderDto>>("/order/v1/orders");

    public Task<OrderDto> GetOrderAsync(Guid id) => GetAsync<OrderDto>($"/order/v1/orders/{id}");

    // ---- payment fault switch ---------------------------------------------
    public Task<FaultStateDto> GetFailureStateAsync() => GetAsync<FaultStateDto>("/payment/v1/simulate-failure");

    public Task<FaultStateDto> SetFailureStateAsync(bool enabled) =>
        PostReadAsync<FaultStateDto>("/payment/v1/simulate-failure", new SimulateFailureRequest(enabled));

    // ---- plumbing ----------------------------------------------------------
    private Task<T> GetAsync<T>(string url, bool auth = true) => SendReadAsync<T>(HttpMethod.Get, url, null, auth);
    private Task<T> PostReadAsync<T>(string url, object body, bool auth = true) => SendReadAsync<T>(HttpMethod.Post, url, body, auth);

    private async Task<T> SendReadAsync<T>(HttpMethod method, string url, object? body, bool auth = true)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body is not null)
            req.Content = JsonContent.Create(body);

        if (auth)
        {
            var token = _ctx.HttpContext?.Session.GetString(SessionExtensions.TokenKey);
            if (!string.IsNullOrEmpty(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var resp = await _http.SendAsync(req);

        if (!resp.IsSuccessStatusCode)
            throw new GatewayException((int)resp.StatusCode, await ExtractErrorAsync(resp));

        var result = await resp.Content.ReadFromJsonAsync<T>(Json);
        return result!;
    }

    private static async Task<string> ExtractErrorAsync(HttpResponseMessage resp)
    {
        if (resp.StatusCode == HttpStatusCode.Unauthorized) return "You need to sign in to do that.";
        if (resp.StatusCode == HttpStatusCode.Forbidden) return "You don't have permission to do that.";
        if (resp.StatusCode == (HttpStatusCode)429) return "Too many attempts. Please wait a moment and try again.";

        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String) return d.GetString()!;
            if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String) return t.GetString()!;
        }
        catch { /* non-JSON body */ }

        return $"Request failed ({(int)resp.StatusCode}).";
    }
}
