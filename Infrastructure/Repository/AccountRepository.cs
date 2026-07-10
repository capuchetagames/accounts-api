using Core.Entity;
using Core.Repository;

namespace Infrastructure.Repository;

public class AccountRepository : EfRepository<User>, IAccountRepository
{
    public AccountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public User? GetUserByEmail(string email)
    {
        var user = DbSet.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

        return user;

    }

    public User? GetUserByName(string username)
    {
        var user = DbSet.FirstOrDefault(u=>u.Name.ToLower() == username.ToLower());
        
        return user;
    }

    public User? GetUserByCpf(string cpf)
    {
        var user = DbSet.FirstOrDefault(u => u.Cpf == cpf);
        
        return user;
    }
}