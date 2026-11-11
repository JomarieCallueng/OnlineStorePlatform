using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.API.Clients;
using Order.API.Data;
using Order.API.Models;
using Store.Shared.Common.Correlation;
using Store.Shared.Common.Errors;
using Store.Shared.Common.Messaging;
using Store.Shared.Common.Security;
using Store.Shared.Events;

namespace Order.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("order/v{version:apiVersion}/orders")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly ICatalogClient _catalog;
    private readonly IEventPublisher _publisher;
    private readonly CorrelationContext _correlation;

    public OrdersController(OrderDbContext db, ICatalogClient catalog, IEventPublisher publisher, CorrelationContext correlation)
    {
        _db = db;
        _catalog = catalog;
        _publisher = publisher;
        _correlation = correlation;
    }

    /// <summary>Place an order (authenticated customer). Starts the saga; returns 202 with the Pending order.</summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Place(PlaceOrderRequest req)
    {
        if (req.Items is null || req.Items.Count == 0)
            throw new StoreValidationException("An order must contain at least one item.");

        var customerId = GetUserId();
        var lines = new List<OrderLine>();
        decimal total = 0;

        foreach (var item in req.Items)
        {
            if (item.Quantity <= 0)
                throw new StoreValidationException("Item quantity must be greater than zero.");

            var product = await _catalog.GetProductAsync(item.ProductId, HttpContext.RequestAborted)
                          ?? throw new NotFoundException($"Product {item.ProductId} was not found.");

            if (!product.IsActive)
                throw new StoreValidationException($"Product '{product.Name}' is not available for purchase.");

            lines.Add(new OrderLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
            total += product.Price * item.Quantity;
        }

        var order = new Models.Order { CustomerId = customerId, Status = OrderStatus.Pending, Total = total, Lines = lines };
        order.History.Add(new OrderStatusHistory { OrderId = order.Id, Status = "Pending", Note = "Order created" });

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _publisher.PublishAsync(new OrderPlaced
        {
            OrderId = order.Id,
            CustomerId = customerId,
            Total = total,
            Items = lines.Select(l => new OrderItem(l.ProductId, l.Quantity, l.UnitPrice)).ToList(),
            CorrelationId = _correlation.CorrelationId
        }, HttpContext.RequestAborted);

        return Accepted($"/order/v1/orders/{order.Id}", ToDto(order));
    }

    /// <summary>Get an order with its saga status history. Customers see only their own orders.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id)
    {
        var order = await _db.Orders.Include(o => o.Lines).Include(o => o.History).AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == id)
                    ?? throw new NotFoundException($"Order {id} was not found.");

        if (!User.IsInRole(StoreRoles.Admin) && order.CustomerId != GetUserId())
            throw new UnauthorizedAccessException("You can only view your own orders.");

        return Ok(ToDto(order));
    }

    /// <summary>List orders. Customers see their own; admins see all.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> List()
    {
        var query = _db.Orders.Include(o => o.Lines).Include(o => o.History).AsNoTracking().AsQueryable();
        if (!User.IsInRole(StoreRoles.Admin))
        {
            var uid = GetUserId();
            query = query.Where(o => o.CustomerId == uid);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(ToDto));
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("Token is missing a valid user id.");
    }

    private static OrderDto ToDto(Models.Order o) => new(
        o.Id,
        o.CustomerId,
        o.Status.ToString(),
        o.Total,
        o.CreatedAt,
        o.Lines.Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList(),
        o.History.OrderBy(h => h.Timestamp).Select(h => new OrderStatusHistoryDto(h.Status, h.Note, h.Timestamp)).ToList());
}
