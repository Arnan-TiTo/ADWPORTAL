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
            bool needSave = false;

            // 1. Seed User
            User? user = await context.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                user = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456yjm"),
                    Fullname = "Admin Tester",
                    Email = "admin@demo.com",
                    Phone = "0800000000",
                    Role = "Admin",
                };
                context.Users.Add(user);
                needSave = true;
            }

            // 2. Save user to generate ID
            if (needSave)
                await context.SaveChangesAsync();

            // 3. Seed Location (ต้องมี User ก่อน)
            Location? location = await context.Locations.FirstOrDefaultAsync();
            if (location == null && user != null)
            {
                location = new Location
                {
                    UserId = user.Id,
                    Name = "Warehouse A",
                    Latitude = 13.7563f,
                    Longitude = 100.5018f,
                    CreatedAt = DateTime.UtcNow
                };
                context.Locations.Add(location);
                await context.SaveChangesAsync();
            }

            // 4. Seed Product (ต้องมี User และ Location ก่อน)
            if (!await context.Products.AnyAsync() && user != null && location != null)
            {
                context.Products.AddRange(
                    new Product
                    {
                        Name = "Notebook",
                        Description = "Demo product notebook",
                        Sku = "SKU001",
                        Quantity = 100,
                        UserId = user.Id,
                        LocationId = location.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Mouse",
                        Description = "Wireless Mouse",
                        Sku = "SKU002",
                        Quantity = 200,
                        UserId = user.Id,
                        LocationId = location.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Keyboard",
                        Description = "Mechanical Keyboard",
                        Sku = "SKU003",
                        Quantity = 150,
                        UserId = user.Id,
                        LocationId = location.Id,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                await context.SaveChangesAsync();
            }

            // 5. Reload 1 product
            var product = await context.Products.FirstOrDefaultAsync();

            // 6. Seed LocationImages
            if (location != null && !await context.LocationImages.AnyAsync())
            {
                context.LocationImages.AddRange(
                    new LocationImage { LocationId = location.Id, ImageUrl = "/uploads/demo1.jpg" },
                    new LocationImage { LocationId = location.Id, ImageUrl = "/uploads/demo2.jpg" },
                    new LocationImage { LocationId = location.Id, ImageUrl = "/uploads/demo3.jpg" }
                );
                await context.SaveChangesAsync();
            }

            // 7. Seed Inventory
            if (product != null && !await context.Inventories.AnyAsync())
            {
                context.Inventories.Add(new Inventory
                {
                    ProductId = product.Id,
                    Quantity = 100
                });
                await context.SaveChangesAsync();
            }

            // 8. Seed StockMovements
            if (product != null && !await context.StockMovements.AnyAsync())
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
                await context.SaveChangesAsync();
            }
        }
    }
}
