using Iconizer.Domain;
using Iconizer.Infrastructure.Services;

namespace Iconizer.Application.Services
{
    public class IconAssignmentService : IIconAssignmentService
    {
        private readonly IFileIconService _fileIconService;
        private readonly ICleaner _cleaner;

        public IconAssignmentService(IFileIconService fileIconService, ICleaner cleaner)
        {
            _fileIconService = fileIconService;
            _cleaner = cleaner;
        }

        public void ApplyIcons(ConfigData config)
        {
            // Aquí podrías paralelizar, controlar excepciones, etc.
            _fileIconService.AssignIconsToFolders(config);
        }

        public void CleanDesktop()
        {
            var desktopFolders = _fileIconService.GetDesktopFolders();
            _cleaner.Clean(desktopFolders);
        }
    }
}