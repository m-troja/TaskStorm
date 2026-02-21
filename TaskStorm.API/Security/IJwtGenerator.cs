using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskStorm.Model.Entity;
using TaskStorm.Log;

namespace TaskStorm.Security;

public interface IJwtGenerator
{
    public AccessToken GenerateAccessToken(int userId);

    public RefreshToken GenerateRefreshToken(int userId);

}
