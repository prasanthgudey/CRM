using CRM.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace CRM.Server.Data
{
    public static class UsersSeedData
    {
        public static async Task SeedUsersAsync(
            UserManager<ApplicationUser> userManager)
        {
            var usersToSeed = new List<(string Email, string Password, string FullName, string Role)>
            {
                ("admin@crm.com",   "Admin@123",   "System Admin",   "Admin"),
                ("manager@crm.com", "Manager@123", "System Manager", "Manager"),
                ("user@crm.com",    "User@123",    "System User",    "User"),
                ("pk@crm.com","Pk@123","prasanth","Admin")

            };

            foreach (var (email, password, fullName, role) in usersToSeed)
            {
                await CreateUserIfNotExists(userManager, email, password, fullName, role);
            }
        }

        private static async Task CreateUserIfNotExists(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string fullName,
            string role)
        {
            // FRAMEWORK: UserManager<T>.FindByEmailAsync
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
                return;

            // USER-DEFINED: ApplicationUser
            var user = new ApplicationUser
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                IsActive = true
            };

            // FRAMEWORK: UserManager<T>.CreateAsync
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // FRAMEWORK: UserManager<T>.AddToRoleAsync
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
