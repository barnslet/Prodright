using System.Threading;
using System.Threading.Tasks;

namespace Prodright
{
    public interface IAccessTokenProvider
    {
        Task<string> GetTokenAsync(CancellationToken ct = default);
    }
}
