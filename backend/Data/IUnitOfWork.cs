using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace AdvancedOrderSystem.Data;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync();

    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);

    Task SaveChangesAsync();
}
