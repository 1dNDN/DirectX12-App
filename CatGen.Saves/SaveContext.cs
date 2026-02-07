using CatGen.DTOs;

using Microsoft.EntityFrameworkCore;

namespace CatGen.Saves;

public class SaveContext : DbContext
{
    public DbSet<ModelOnDisk> ModelsOnDisk => Set<ModelOnDisk>();

    public DbSet<SpawnedObject> SpawnedObjects => Set<SpawnedObject>();

    public SaveContext()
    {
        Database.OpenConnection();
        Database.Migrate();
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=catgen.db");
    }
}
