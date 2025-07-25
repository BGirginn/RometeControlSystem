using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RemoteControl.Core.Data;

/// <summary>
/// Design-time factory for RemoteControlDbContext
/// </summary>
public class RemoteControlDbContextFactory : IDesignTimeDbContextFactory<RemoteControlDbContext>
{
    public RemoteControlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RemoteControlDbContext>();
        
        // Use LocalDB for design-time operations
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RemoteControlSystem;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;");
        
        return new RemoteControlDbContext(optionsBuilder.Options);
    }
}
