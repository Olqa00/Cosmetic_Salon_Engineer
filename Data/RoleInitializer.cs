using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
namespace Engineer_MVC.Data
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("User"))
            {
                var role = new IdentityRole("User");
                await roleManager.CreateAsync(role);
            }
            if (!await roleManager.RoleExistsAsync("SuperUser"))
            {
                var role = new IdentityRole("SuperUser");
                await roleManager.CreateAsync(role);
            }
            if (!await roleManager.RoleExistsAsync("OutEmployee"))
            {
                var role = new IdentityRole("OutEmployee");
                await roleManager.CreateAsync(role);
            }
            if (!await roleManager.RoleExistsAsync("Employee"))
            {
                var role = new IdentityRole("Employee");
                await roleManager.CreateAsync(role);
            }
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                var role = new IdentityRole("Admin");
                await roleManager.CreateAsync(role);
            }
        }
    }
}
