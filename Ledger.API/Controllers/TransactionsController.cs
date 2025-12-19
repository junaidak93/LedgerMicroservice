using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ledger.API.DTOs;
using Ledger.API.Services;
using Ledger.API.Helpers;

namespace Ledger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("TransactionPolicy")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IAuditService _auditService;

    public TransactionsController(
        ITransactionService transactionService,
        IAuditService auditService)
    {
        _transactionService = transactionService;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> CreateTransaction([FromBody] TransactionCreateDto dto)
    {
        try
        {
            var userId = ClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _transactionService.CreateTransactionAsync(userId.Value, dto);
            
            await _auditService.LogAsync(
                "CreateTransaction",
                "Transaction",
                result.Id,
                userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()
            );

            return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    //[Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetAllTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _transactionService.GetAllTransactionsAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransaction(Guid id)
    {
        try
        {
            var userId = ClaimsHelper.GetUserId(User);
            var isAdmin = ClaimsHelper.IsAdmin(User);
            
            var result = await _transactionService.GetTransactionByIdAsync(id, userId, isAdmin);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> UpdateTransaction(
        Guid id,
        [FromBody] TransactionUpdateDto dto)
    {
        try
        {
            var userId = ClaimsHelper.GetUserId(User);
            var isAdmin = ClaimsHelper.IsAdmin(User);
            
            var result = await _transactionService.UpdateTransactionAsync(id, dto, userId, isAdmin);
            
            await _auditService.LogAsync(
                "UpdateTransaction",
                "Transaction",
                id,
                userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        try
        {
            await _transactionService.DeleteTransactionAsync(id);
            
            var userId = ClaimsHelper.GetUserId(User);
            await _auditService.LogAsync(
                "DeleteTransaction",
                "Transaction",
                id,
                userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()
            );

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

}

