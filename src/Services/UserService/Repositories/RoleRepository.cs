using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Entities;

namespace UserService.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int roleId);
    Task<Role?> GetByNameAsync(string roleName);
    Task<List<Role>> GetAllAsync();
}

public class RoleRepository : IRoleRepository
{
    private readonly UserDbContext _context;

    public RoleRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(int roleId)
    {
        return await _context.Roles.FindAsync(roleId);
    }

    public async Task<Role?> GetByNameAsync(string roleName)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }
}
