using CRM.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace CRM.Server.Data
{
    public static class AdminSeedData
    {
        public static async Task SeedAdminUserAsync(
            UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "adminn@crm.com";
            var adminPassword = "Admin@123";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                var user = new ApplicationUser
                {
                    FullName = "System Admin",
                    Email = adminEmail,
                    UserName = adminEmail,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}
