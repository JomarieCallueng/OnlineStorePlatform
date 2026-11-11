namespace Storefront.UI.Services;

public sealed class GatewayException : Exception
{
    public int StatusCode { get; }
    public GatewayException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}
