using DataAccess.Entities;

namespace DataAccess.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public User? GetById(int id) => db.Users.Find(id);

    public User? GetByUsername(string username) =>
        db.Users.FirstOrDefault(u => u.Username == username);

    public List<User> GetAll() => db.Users.OrderBy(u => u.Id).ToList();

    public bool Create(User user)
    {
        if (db.Users.Any(u => u.Username == user.Username)) return false;
        db.Users.Add(user);
        db.SaveChanges();
        return true;
    }

    public void Update(User user)
    {
        db.Users.Update(user);
        db.SaveChanges();
    }

    public void Delete(User user)
    {
        db.Users.Remove(user);
        db.SaveChanges();
    }
}