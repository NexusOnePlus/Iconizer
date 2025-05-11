using System.Threading.Tasks;

namespace Iconizer.Application.Services
{
    public interface ITrayIconService
    {
        Task InitializeAsync();
        void Dispose();
    }
}