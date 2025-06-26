using Humanizer;
using Microsoft.EntityFrameworkCore;
using miniApp.API.Models;
using System;
using System.Threading.Tasks;

namespace miniApp.API.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.Users.AnyAsync())
            {

               context.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Fullname = "Admin Tester",
                    Email = "admin@demo.com",
                    Phone = "0800000000",
                    Role = RoleType.Staff
                });
            }

            if (!await context.Products.AnyAsync())
            {
                context.Products.AddRange(
                    new Product { Name = "Notebook", Description = "Demo product notebook" },
                    new Product { Name = "Mouse", Description = "Wireless Mouse" },
                    new Product { Name = "Keyboard", Description = "Mechanical Keyboard" }
                );
            }

            // Get userId และ productId ที่จะใช้
            var user = await context.Users.FirstOrDefaultAsync();
            var product = await context.Products.FirstOrDefaultAsync();

            if (user != null && product != null)
            {
                if (!await context.Locations.AnyAsync())
                {
                    context.Locations.Add(new Location
                    {
                        UserId = user.Id,
                        Name = "Warehouse A",
                        Latitude = 13.7563f,
                        Longitude = 100.5018f,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (!await context.Inventories.AnyAsync())
                {
                    context.Inventories.Add(new Inventory
                    {
                        ProductId = product.Id,
                        Quantity = 100
                    });
                }
            }

            var location = await context.Locations.FirstOrDefaultAsync();
            if (location != null && !await context.LocationImages.AnyAsync())
            {
                context.LocationImages.AddRange(
                    new LocationImage { LocationId = location.Id, ImageUrl = "/uploads/demo1.jpg" },
                    new LocationImage { LocationId = location.Id, ImageUrl = "/uploads/demo2.jpg" },
                    new LocationImage { LocationId = location.Id, ImageUrl = "/uploads/demo3.jpg" }
                );
            }

            if (!await context.StockMovements.AnyAsync())
            {
                context.StockMovements.AddRange(
                    new StockMovement
                    {
                        ProductId = product.Id,
                        Type = "IN",
                        Quantity = 50,
                        Timestamp = DateTime.UtcNow.AddDays(-2)
                    },
                    new StockMovement
                    {
                        ProductId = product.Id,
                        Type = "COUNT",
                        Quantity = 90,
                        Timestamp = DateTime.UtcNow.AddDays(-1)
                    },
                    new StockMovement
                    {
                        ProductId = product.Id,
                        Type = "ADJUST",
                        Quantity = 100,
                        Timestamp = DateTime.UtcNow
                    }
                );
            }


            await context.SaveChangesAsync();
        }
    }
}
