using Microsoft.EntityFrameworkCore;
using TaskStorm.Data;
using TaskStorm.Model.Entity;

namespace TaskStorm.Service.Impl;

public class RoleService : IRoleService
{
    private readonly PostgresqlDbContext _db;
    public async Task<Role> GetRoleByName(string name)
    {
        Task<Role> role = _db.Roles.Where(r => r.Name == name).FirstAsync();
        return await role;
    }

    public RoleService(PostgresqlDbContext db)
    {
        _db = db;
    }
}
