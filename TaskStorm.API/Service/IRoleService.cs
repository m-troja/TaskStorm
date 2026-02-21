using TaskStorm.Model.Entity;

namespace TaskStorm.Service
{
    public interface IRoleService
    {
        Task<Role> GetRoleByName(string name);
    }
}

