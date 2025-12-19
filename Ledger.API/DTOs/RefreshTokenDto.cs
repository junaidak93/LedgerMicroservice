using System.ComponentModel.DataAnnotations;

namespace Ledger.API.DTOs;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

