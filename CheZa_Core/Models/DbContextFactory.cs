using CheZa_Core.Data;
using Microsoft.EntityFrameworkCore;

namespace CheZa_Core.Models
{
    public static class DbContextFactory
    {
        private static readonly string ConnectionString = "Host=localhost;Database=choza_db;Username=postgres;Password=password";
        //host.docker.internal Host=db;Port=5432;
        public static ApplicationDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(ConnectionString);
            optionsBuilder.EnableSensitiveDataLogging();

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
