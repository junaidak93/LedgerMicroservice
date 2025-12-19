using Ledger.API.DTOs;
using Ledger.API.Models;
using Ledger.API.Repositories;

namespace Ledger.API.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILoginRepository _loginRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ILoginRepository loginRepository)
    {
        _transactionRepository = transactionRepository;
        _loginRepository = loginRepository;
    }

    public async Task<TransactionResponseDto> CreateTransactionAsync(Guid userId, TransactionCreateDto dto)
    {
        var user = await _loginRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Calculate net amount change
        var netAmount = dto.Type == TransactionType.Incoming 
            ? dto.Amount - dto.Fee 
            : -(dto.Amount + dto.Fee);

        // Check if balance would go negative
        if (user.Balance + netAmount < 0)
        {
            throw new InvalidOperationException("Insufficient balance");
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = dto.Amount,
            Type = dto.Type,
            Fee = dto.Fee,
            Description = dto.Description,
            Timestamp = dto.Timestamp ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update cached balance
        user.Balance += netAmount;
        user.UpdatedAt = DateTime.UtcNow;

        await _transactionRepository.CreateAsync(transaction);
        await _loginRepository.UpdateAsync(user);

        return MapToDto(transaction);
    }

    public async Task<TransactionResponseDto?> GetTransactionByIdAsync(Guid transactionId, Guid? requestingUserId, bool isAdmin)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            return null;
        }

        // Check authorization
        if (!isAdmin && transaction.UserId != requestingUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this transaction");
        }

        return MapToDto(transaction);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetUserTransactionsAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
    {
        var transactions = await _transactionRepository.GetByUserIdAsync(userId, pageNumber, pageSize);
        return transactions.Select(MapToDto);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetAllTransactionsAsync(int pageNumber = 1, int pageSize = 10)
    {
        var transactions = await _transactionRepository.GetAllAsync(pageNumber, pageSize);
        return transactions.Select(MapToDto);
    }

    public async Task<TransactionResponseDto> UpdateTransactionAsync(Guid transactionId, TransactionUpdateDto dto, Guid? requestingUserId, bool isAdmin)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        // Check authorization
        if (!isAdmin && transaction.UserId != requestingUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to update this transaction");
        }

        var user = await _loginRepository.GetByIdAsync(transaction.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Revert old transaction impact
        var oldNetAmount = transaction.Type == TransactionType.Incoming
            ? transaction.Amount - transaction.Fee
            : -(transaction.Amount + transaction.Fee);
        user.Balance -= oldNetAmount;

        // Calculate new net amount change
        var newNetAmount = dto.Type == TransactionType.Incoming
            ? dto.Amount - dto.Fee
            : -(dto.Amount + dto.Fee);

        // Check if balance would go negative
        if (user.Balance + newNetAmount < 0)
        {
            throw new InvalidOperationException("Insufficient balance");
        }

        // Update transaction
        transaction.Amount = dto.Amount;
        transaction.Type = dto.Type;
        transaction.Fee = dto.Fee;
        transaction.Description = dto.Description;
        transaction.Timestamp = dto.Timestamp ?? transaction.Timestamp;
        transaction.UpdatedAt = DateTime.UtcNow;

        // Update cached balance
        user.Balance += newNetAmount;
        user.UpdatedAt = DateTime.UtcNow;

        await _transactionRepository.UpdateAsync(transaction);
        await _loginRepository.UpdateAsync(user);

        return MapToDto(transaction);
    }

    public async Task DeleteTransactionAsync(Guid transactionId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        var user = await _loginRepository.GetByIdAsync(transaction.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Revert transaction impact on balance
        var netAmount = transaction.Type == TransactionType.Incoming
            ? transaction.Amount - transaction.Fee
            : -(transaction.Amount + transaction.Fee);
        user.Balance -= netAmount;
        user.UpdatedAt = DateTime.UtcNow;

        await _transactionRepository.DeleteAsync(transactionId);
        await _loginRepository.UpdateAsync(user);
    }

    private static TransactionResponseDto MapToDto(Transaction transaction)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Fee = transaction.Fee,
            Description = transaction.Description,
            Timestamp = transaction.Timestamp,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}

