using Microsoft.EntityFrameworkCore;
using Soluvion.API.Models;

namespace Soluvion.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies{ get; set; }
        public DbSet<Service> Services{ get; set; }
        public DbSet<GalleryCategory> GalleryCategories{ get; set; }
        public DbSet<GalleryImage> GalleryImages{ get; set; }
        public DbSet<User> Users{ get; set; }
    }
}
