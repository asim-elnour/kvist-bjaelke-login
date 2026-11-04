using DataAccess;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Security.Tests;

public class UserRepositoryTests
{
    internal static UserRepository CreateRepository()
    {
        // A database that only lives in memory, so every test starts empty.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return new UserRepository(db);
    }

    private static User NewUser(string username) =>
        new() { Username = username, PasswordHash = "hash", Role = "user" };

    [Fact]
    public void Create_SavesTheUser()
    {
        var repository = CreateRepository();

        Assert.True(repository.Create(NewUser("alice")));

        Assert.NotNull(repository.GetByUsername("alice"));
    }

    [Fact]
    public void Create_RefusesAUsernameThatIsTaken()
    {
        var repository = CreateRepository();
        repository.Create(NewUser("alice"));

        Assert.False(repository.Create(NewUser("alice")));

        Assert.Single(repository.GetAll());
    }
}