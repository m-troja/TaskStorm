using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;

namespace TaskStorm.Service
{
    public interface IRegisterService
    {
        public Task<User> Register(RegistrationRequest rr);
    }
}
