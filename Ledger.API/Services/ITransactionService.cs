using Ledger.API.DTOs;

namespace Ledger.API.Services;

public interface ITransactionService
{
    Task<TransactionResponseDto> CreateTransactionAsync(Guid userId, TransactionCreateDto dto, string? idempotencyKey = null);
    Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid transactionId, Guid? requestingUserId, bool isAdmin);
    Task<IEnumerable<TransactionResponseDto>> GetUserTransactionsAsync(Guid userId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<TransactionResponseDto>> GetAllTransactionsAsync(int pageNumber = 1, int pageSize = 10);
    Task<TransactionResponseDto> UpdateTransactionAsync(Guid transactionId, TransactionUpdateDto dto, Guid? requestingUserId, bool isAdmin);
    Task DeleteTransactionAsync(Guid transactionId);
}

