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

    public PasswordService(ILogger<PasswordService> logger)
    {
        l = logger;
    }
}
