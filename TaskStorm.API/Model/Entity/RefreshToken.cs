namespace TaskStorm.Model.Entity;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime Expires { get; set; }
    public bool IsRevoked { get; set; }
    public User User { get; set; } = null!;
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public RefreshToken(string token, int userId, DateTime expires)
    {
        Token = token;
        UserId = userId;
        Expires = expires;
        IsRevoked = false;
    }

    public RefreshToken() { } // EF
}