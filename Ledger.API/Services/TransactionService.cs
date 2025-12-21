using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ledger.API.DTOs;
using Ledger.API.Models;
using Ledger.API.Repositories;
using Ledger.API.Data;

namespace Ledger.API.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILoginRepository _loginRepository;
    private readonly ApplicationDbContext _context;
    private readonly IIdempotencyRepository _idempotencyRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ILoginRepository loginRepository,
        ApplicationDbContext context,
        IIdempotencyRepository idempotencyRepository)
    {
        _transactionRepository = transactionRepository;
        _loginRepository = loginRepository;
        _context = context;
        _idempotencyRepository = idempotencyRepository;
    }

    public async Task<TransactionResponseDto> CreateTransactionAsync(Guid userId, TransactionCreateDto dto, string? idempotencyKey = null)
    {
        // If idempotencyKey provided, ensure we haven't already processed it
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _idempotencyRepository.GetByKeyAsync(idempotencyKey);
            if (existing != null)
            {
                // Return stored response
                try
                {
                    var cached = JsonSerializer.Deserialize<TransactionResponseDto>(existing.ResponseBody);
                    if (cached != null) return cached;
                }
                catch { /* fall through and reprocess if deserialization fails */ }
            }
        }

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

        // Use a DB transaction to atomically create transaction, update balance, and store idempotency
        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
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

            // Persist changes
            _context.Transactions.Add(transaction);
            _context.Logins.Update(user);
            await _context.SaveChangesAsync();

            var result = MapToDto(transaction);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var entry = new IdempotencyKey
                {
                    Id = Guid.NewGuid(),
                    Key = idempotencyKey,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    ResponseBody = JsonSerializer.Serialize(result),
                    StatusCode = 201,
                    ExpiresAt = DateTime.UtcNow.AddDays(1)
                };

                // Use repository to persist idempotency entry (shares same DbContext scope)
                // If a concurrent request already inserted the same key, catch the
                // DB update exception, rollback this transaction and return the
                // existing cached response to avoid duplicate processing.
                    try
                    {
                        await _idempotencyRepository.CreateAsync(entry);
                    }
                catch (DbUpdateException)
                {
                    await dbTx.RollbackAsync();

                    var existingEntry = await _idempotencyRepository.GetByKeyAsync(idempotencyKey);
                    if (existingEntry != null)
                    {
                        try
                        {
                            var cached = JsonSerializer.Deserialize<TransactionResponseDto>(existingEntry.ResponseBody);
                            if (cached != null) return cached;
                        }
                        catch
                        {
                            // fall through and rethrow below
                        }
                    }

                    throw;
                }
            }

            await dbTx.CommitAsync();

            return MapToDto(transaction);
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }
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

