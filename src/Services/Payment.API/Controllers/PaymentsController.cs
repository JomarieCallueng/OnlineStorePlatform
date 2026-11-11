using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;
using Payment.API.Models;
using Store.Shared.Common.Security;

namespace Payment.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("payment/v{version:apiVersion}")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly PaymentFaultSwitch _fault;
    private readonly ILogger<PaymentsController> _log;

    public PaymentsController(PaymentDbContext db, PaymentFaultSwitch fault, ILogger<PaymentsController> log)
    {
        _db = db;
        _fault = fault;
        _log = log;
    }

    /// <summary>
    /// Toggle the "simulate payment failure" switch at runtime. While enabled every charge is
    /// declined, which drives the saga down its compensation path. Requires authentication.
    /// </summary>
    [HttpPost("simulate-failure")]
    [Authorize]
    public ActionResult<FaultStateDto> SetSimulateFailure(SimulateFailureRequest req)
    {
        _fault.FailPayments = req.Enabled;
        _log.LogWarning("Payment fault switch set to {State} by {User}", req.Enabled, User.Identity?.Name);
        return Ok(new FaultStateDto(_fault.FailPayments));
    }

    /// <summary>Read the current fault-switch state.</summary>
    [HttpGet("simulate-failure")]
    [Authorize]
    public ActionResult<FaultStateDto> GetSimulateFailure() => Ok(new FaultStateDto(_fault.FailPayments));

    /// <summary>List charges for an order (Admin).</summary>
    [HttpGet("charges/{orderId:guid}")]
    [Authorize(Roles = StoreRoles.Admin)]
    public async Task<ActionResult<IEnumerable<ChargeDto>>> GetCharges(Guid orderId)
    {
        var charges = await _db.Charges.AsNoTracking()
            .Where(c => c.OrderId == orderId)
            .Select(c => new ChargeDto(c.Id, c.OrderId, c.Amount, c.Status.ToString(), c.CreatedAt))
            .ToListAsync();
        return Ok(charges);
    }
}
