using System;
using System.Threading.Tasks;

namespace Iconizer.Application.Services
{
    public interface IDesktopWatcher : IDisposable
    {
        /// <summary>
        /// Inicia la vigilancia de cambios.
        /// </summary>
        Task StartAsync();
        /// <summary>
        /// Recarga la configuración y reinicia watchers.
        /// </summary>
        Task ReloadAsync();
    }
}