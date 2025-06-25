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

        // เพิ่ม DbSet ที่คุณใช้ เช่น:
        public DbSet<User> Users { get; set; }
        public DbSet<QrLoginInfo> QrLoginInfos { get; set; }
    }
}
