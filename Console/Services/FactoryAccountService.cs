
using Data;
using Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class FactoryAccountService
{

    private readonly AppDbContext _context;

    public FactoryAccountService(AppDbContext context)
    {
        _context = context;
    }

    public IAccountService Create()
    {
        bool isRelational = _context.Database.IsRelational();

        if (isRelational)
            return new AccountServiceContext(_context);
        return new AccountServiceMemory(_context);
    }
}
