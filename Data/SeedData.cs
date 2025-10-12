using Microsoft.AspNetCore.Identity;
using DriveHub.Models;

namespace DriveHub.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roleNames = { "Admin", "Instructor", "Student" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed Admin - Mr Gumede
        var admin = new ApplicationUser
        {
            UserName = "goodenough245@gmail.com",
            Email = "goodenough245@gmail.com",
            FullName = "Anele Gumede",
            IDNumber = "0000000000000",
            Address = "Admin Address",
            EmailConfirmed = true
        };
        string adminPass = "Drivehub@1";
        var adminUser = await userManager.FindByEmailAsync(admin.Email);
        if (adminUser == null)
        {
            var createAdmin = await userManager.CreateAsync(admin, adminPass);
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // Seed Instructor 1 - Instructor Puling
        var instructor1 = new ApplicationUser
        {
            UserName = "22416181@dut4life.ac.za",
            Email = "22416181@dut4life.ac.za",
            FullName = "Molefe Puling",
            IDNumber = "1111111111111",
            Address = "Instructor Address 1",
            EmailConfirmed = true
        };
        string inst1Pass = "Instructor@1";
        var inst1User = await userManager.FindByEmailAsync(instructor1.Email);
        if (inst1User == null)
        {
            var createInst1 = await userManager.CreateAsync(instructor1, inst1Pass);
            if (createInst1.Succeeded)
            {
                await userManager.AddToRoleAsync(instructor1, "Instructor");
            }
        }

        // Seed Instructor 2 - Instructor Mkhize
        var instructor2 = new ApplicationUser
        {
            UserName = "22109475@dut4life.ac.za",
            Email = "22109475@dut4life.ac.za",
            FullName = "Mrs Mkhize",
            IDNumber = "2222222222222",
            Address = "Instructor Address 2",
            EmailConfirmed = true
        };
        string inst2Pass = "Instructor@2";
        var inst2User = await userManager.FindByEmailAsync(instructor2.Email);
        if (inst2User == null)
        {
            var createInst2 = await userManager.CreateAsync(instructor2, inst2Pass);
            if (createInst2.Succeeded)
            {
                await userManager.AddToRoleAsync(instructor2, "Instructor");
            }
        }
    }
}