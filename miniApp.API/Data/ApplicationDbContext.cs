using Microsoft.EntityFrameworkCore;
using miniApp.API.Models;

namespace miniApp.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<QrLoginInfo> QrLoginInfos { get; set; }
    }
}
