using Microsoft.AspNetCore.Identity;

namespace CRM.Server.Data
{
    public static class RoleSeedData
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Manager", "User" };

            foreach (var role in roles)
            {
                // ✅ Built-in Identity check
                if (!await roleManager.RoleExistsAsync(role))
                {
                    // ✅ Built-in Identity role creation
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}

