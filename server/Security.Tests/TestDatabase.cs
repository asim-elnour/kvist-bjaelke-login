using DataAccess;
using DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Security.Tests;

public static class TestDatabase
{
    public static UserRepository CreateRepository()
    {
        // A database that only lives in memory, so every test starts empty.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return new UserRepository(db);
    }
}
