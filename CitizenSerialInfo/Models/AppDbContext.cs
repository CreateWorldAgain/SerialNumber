using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public DbSet<ImportFileInfo> ImportFileInfo { get; set; }
        public DbSet<SerialInfo> SerialInfo { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> option) : base(option)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.Ignore(i => i.LockoutEnabled)
                    .Ignore(i => i.LockoutEnd)                    
                    .Ignore(i => i.PhoneNumber)
                    .Ignore(i => i.PhoneNumberConfirmed)
                    .Ignore(i => i.TwoFactorEnabled)
                    .Ignore(i => i.AccessFailedCount)
                    .Ignore(i => i.ConcurrencyStamp);
            });

            builder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("Roles");
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });
        }

        async public Task EnsureSeedData(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogger logger)
        {
            
            var role = await roleManager.FindByNameAsync("Administrator");
            if (role == null)
            {
                var result = await roleManager.CreateAsync(new ApplicationRole { Name = "Administrator" });
                if (!result.Succeeded)
                {
                    string errorText = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                    logger.LogError(errorText);
                    throw new Exception(errorText);
                }
            }

            var user = await userManager.FindByNameAsync("ADMIN");
            if (user == null)
            {
                var result = await userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "Admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@domain.de",
                    EmailConfirmed=true,
                    IsApproved=true
                }, "admin");

                if (!result.Succeeded)
                {
                    string errorText = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                    logger.LogError(errorText);
                    throw new Exception(errorText);
                }
                else
                {
                    user = await userManager.FindByNameAsync("ADMIN");
                    var res = await userManager.AddToRoleAsync(user, "Administrator");
                    if (!res.Succeeded)
                    {
                        string errorText = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                        logger.LogError(errorText);
                        throw new Exception(errorText);
                    }
                }
            }

            user = await userManager.FindByNameAsync("Internal");
            if (user == null)
            {
                var result = await userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "Internal",
                    NormalizedUserName = "Internal",
                    Email = "admin@domain.de",
                    EmailConfirmed = true,
                    IsApproved = true
                }, "-CDkK,Wnpu");

                if (!result.Succeeded)
                {
                    string errorText = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                    logger.LogError(errorText);
                    throw new Exception(errorText);
                }
                else
                {
                    user = await userManager.FindByNameAsync("Internal");
                    var res = await userManager.AddToRoleAsync(user, "Administrator");
                    if (!res.Succeeded)
                    {
                        string errorText = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                        logger.LogError(errorText);
                        throw new Exception(errorText);
                    }
                }
            }

            var defaultRole = await roleManager.FindByNameAsync("Default");
            if (defaultRole == null)
            {
                var result = await roleManager.CreateAsync(new ApplicationRole { Name = "Default" });
                if (!result.Succeeded)
                {
                    string errorText = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                    logger.LogError(errorText);
                    throw new Exception(errorText);
                }
            }

            var staffRole = await roleManager.FindByNameAsync("Staff");
            if (staffRole == null)
            {
                var result = await roleManager.CreateAsync(new ApplicationRole { Name = "Staff" });
                if (!result.Succeeded)
                {
                    string errorText = String.Join("\r\n", result.Errors.Select(s => s.Description).ToArray());
                    logger.LogError(errorText);
                    throw new Exception(errorText);
                }
            }
        }
    }
}
