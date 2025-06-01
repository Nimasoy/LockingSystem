using Microsoft.EntityFrameworkCore;

namespace LockingSystem
{
    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string ?Name { get; set; }
        public string ?Email { get; set; }
    }
}
