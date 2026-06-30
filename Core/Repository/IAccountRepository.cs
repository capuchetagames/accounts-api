using Core.Entity;

namespace Core.Repository;

public interface IAccountRepository : IRepository<User>
{ 
    User? GetUserByEmail(string email);
    User? GetUserByName(string username);
    
    User? GetUserByCpf(int cpf);
}