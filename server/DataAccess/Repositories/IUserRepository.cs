using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IUserRepository
{
    User? GetById(int id);
    User? GetByUsername(string username);
    List<User> GetAll();
    bool Create(User user);
    void Update(User user);
    void Delete(User user);
}