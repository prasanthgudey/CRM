using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        // ✅ Built-in DI pattern
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ GET BY ID
        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        // ✅ GET BY EMAIL
        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        // ✅ GET ALL
        public async Task<List<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        // ✅ CREATE
        public async Task AddAsync(ApplicationUser user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        // ✅ UPDATE
        public async Task UpdateAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        // ✅ ✅ ✅ DELETE (MISSING CRUD PART)
        public async Task DeleteAsync(ApplicationUser user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        // ✅ ✅ ✅ FILTER (USED BY /api/user/filter)
        //public async Task<List<ApplicationUser>> FilterAsync(string? role, bool? isActive)
        //{
        //    var query = _context.Users.AsQueryable();

        //    if (!string.IsNullOrWhiteSpace(role))
        //        query = query.Where(x => x.Role == role);

        //    if (isActive.HasValue)
        //        query = query.Where(x => x.IsActive == isActive.Value);

        //    return await query.ToListAsync();
        //}
    }
}
