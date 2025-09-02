using Microsoft.AspNetCore.Identity;

namespace BlazorDemo.Showcase.Data {
    public class DemoData(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager) {

        public static readonly string UserEmail = "admin@dxmail.com";
        public static readonly string UserPassword = "1qaz!QAZ";

        public async Task InitAsync() {
            // Dev-time seeding and repair for the default admin user.
            var existingUser = await userManager.FindByEmailAsync(UserEmail);
            if (existingUser != null) {
                bool updated = false;
                if (!existingUser.EmailConfirmed) { existingUser.EmailConfirmed = true; updated = true; }
                // Clear lockout and failed count to avoid NotAllowed/LockedOut results
                if (existingUser.LockoutEnd != null || existingUser.AccessFailedCount > 0) {
                    existingUser.LockoutEnd = null;
                    existingUser.AccessFailedCount = 0;
                    updated = true;
                }
                if (updated) {
                    await userManager.UpdateAsync(existingUser);
                }

                // Reset password to the known demo value so login works
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                await userManager.ResetPasswordAsync(existingUser, resetToken, UserPassword);

                if (!await roleManager.RoleExistsAsync("Admin")) {
                    var _ = await roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                if (!await userManager.IsInRoleAsync(existingUser, "Admin")) {
                    await userManager.AddToRoleAsync(existingUser, "Admin");
                }
                return;
            }

            var email = UserEmail;
            var user = new ApplicationUser {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                LockoutEnabled = true,
            };
            var createResult = await userManager.CreateAsync(user, UserPassword);
            if (!createResult.Succeeded)
                return;

            // Ensure the Admin role exists
            if (!await roleManager.RoleExistsAsync("Admin")) {
                var _ = await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            // Add the user to Admin role
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}
