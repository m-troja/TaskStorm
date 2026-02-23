using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace TaskStorm.Security;

public class PasswordService : IPasswordService
{
    private readonly ILogger<PasswordService> l;
    public string HashPassword(string password, byte[] salt)
    {
        l.LogDebug($"Hashing password with salt: {Convert.ToBase64String(salt)}");
        string hashedPw = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32));
        l.LogDebug($"Hashed password: {hashedPw}");
        return hashedPw;
    }

    public byte[] GenerateSalt()
    {
        int size = 16;
        var salt = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        l.LogDebug($"Generated salt: {Convert.ToBase64String(salt)}");
        return salt;
    }

    public async Task<Boolean> Verify(string password, string hashedPassword, byte[] salt)
    {
        l.LogDebug($"Verifying password. Input password: {password}, Hashed password: {hashedPassword}, Salt: {Convert.ToBase64String(salt)}");
        string hashedInput = HashPassword(password, salt);
        l.LogDebug($"Hashed input password: {hashedInput}");
        if (hashedInput != hashedPassword)
        {
            l.LogWarning("Password verification failed");
            throw new UnauthorizedAccessException("Invalid password");
        }
        l.LogDebug("Password verification succeeded");
        return true;
    }
    public PasswordService(ILogger<PasswordService> logger)
    {
        l = logger;
    }
}
