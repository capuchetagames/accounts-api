using Core.Entity;
using Core.Repository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class EfRepository<T> : IRepository<T> where T : EntityBase
{
    private readonly ApplicationDbContext _context;
    protected readonly DbSet<T> DbSet;

    protected EfRepository(ApplicationDbContext context)
    {
        _context = context;
        DbSet = _context.Set<T>();
    }
    
    public IList<T> GetAll() => DbSet.ToList();

    public T GetById(Guid id) => DbSet.FirstOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("Invalid ID");

    public void Add(T entity)
    {
        entity.CreatedAt = DateTime.Now;
        DbSet.Add(entity);
        _context.SaveChanges();
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
        _context.SaveChanges();
    }

    public void Delete(Guid id)
    {
        DbSet.Remove(GetById(id));
        _context.SaveChanges();
    }
}