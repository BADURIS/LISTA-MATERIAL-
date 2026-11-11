using System.Threading;
using System.Threading.Tasks;

namespace LMaterial.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
