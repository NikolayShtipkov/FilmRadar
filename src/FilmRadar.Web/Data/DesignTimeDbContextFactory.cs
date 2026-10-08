using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FilmRadar.Web.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FilmRadarDbContext>
{
    public FilmRadarDbContext CreateDbContext(string[] args)
    {
        // EF tooling must not start the web host or contact external services.
        var root = Directory.GetCurrentDirectory();
        var project = Directory.Exists(Path.Combine(root, "src", "FilmRadar.Web"))
            ? Path.Combine(root, "src", "FilmRadar.Web") : root;
        var directory = Path.Combine(project, "App_Data");
        Directory.CreateDirectory(directory);
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__FilmRadar")
            ?? new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "filmradar.db"), ForeignKeys = true }.ToString();
        return new FilmRadarDbContext(new DbContextOptionsBuilder<FilmRadarDbContext>().UseSqlite(connection).Options);
    }
}
