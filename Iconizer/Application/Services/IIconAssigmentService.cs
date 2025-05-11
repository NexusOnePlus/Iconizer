using Iconizer.Domain;

namespace Iconizer.Application.Services
{
    public interface IIconAssignmentService
    {
        /// <summary>
        /// Aplica los iconos a las carpetas según la configuración.
        /// </summary>
        void ApplyIcons(ConfigData config);

        /// <summary>
        /// Limpia los íconos/deja el estado original (por ejemplo, en Desktop).
        /// </summary>
        void CleanDesktop();
    }
}