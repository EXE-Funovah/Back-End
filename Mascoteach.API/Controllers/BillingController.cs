using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class BillingController : BaseController
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet("plans")]
    public IActionResult GetPlans()
    {
        return Ok(_billingService.GetPlans());
    }

    [HttpPost("create-payment-link")]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
    {
        try
        {
            var result = await _billingService.CreatePaymentLinkAsync(CurrentUserId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentBilling()
    {
        var result = await _billingService.GetCurrentBillingAsync(CurrentUserId);
        if (result == null) return NotFound("User does not exist.");
        return Ok(result);
    }

    [HttpGet("orders/me")]
    public async Task<IActionResult> GetMyOrders()
    {
        var result = await _billingService.GetMyOrdersAsync(CurrentUserId);
        return Ok(result);
    }

    [HttpPatch("orders/{orderCode:long}/cancel")]
    public async Task<IActionResult> CancelOrder(long orderCode)
    {
        var success = await _billingService.CancelOrderAsync(CurrentUserId, orderCode);
        if (!success) return BadRequest("Payment order does not exist, does not belong to you, or cannot be cancelled.");
        return Ok("Payment order cancelled.");
    }

    [AllowAnonymous]
    [HttpPost("payos-webhook")]
    public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookRequest request)
    {
        try
        {
            await _billingService.HandlePayOsWebhookAsync(request);
            return Ok(new { message = "Webhook processed." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
