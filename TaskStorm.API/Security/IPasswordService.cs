using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace TaskStorm.Security;

public interface IPasswordService
{
    public string HashPassword(string password, byte[] salt);

    public byte[] GenerateSalt();

}
