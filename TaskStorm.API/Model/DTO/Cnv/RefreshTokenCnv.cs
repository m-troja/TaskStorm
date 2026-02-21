using TaskStorm.Model.Entity;

namespace TaskStorm.Model.DTO.Cnv;

public class RefreshTokenCnv
{
    public RefreshTokenDto EntityToDto( RefreshToken refreshToken)
    {
        return new RefreshTokenDto(
            Token: refreshToken.Token,
            Expires: refreshToken.Expires
        );
    }
}
