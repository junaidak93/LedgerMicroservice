using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ledger.API.DTOs;
using Ledger.API.Services;
using Ledger.API.Helpers;

namespace Ledger.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public UsersController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("{userId}/transactions")]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetUserTransactions(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var requestingUserId = ClaimsHelper.GetUserId(User);
        var isAdmin = ClaimsHelper.IsAdmin(User);

        if (!isAdmin && requestingUserId != userId)
        {
            return Forbid("You do not have permission to access this user's transactions");
        }

        var result = await _transactionService.GetUserTransactionsAsync(userId, pageNumber, pageSize);
        return Ok(result);
    }
}

