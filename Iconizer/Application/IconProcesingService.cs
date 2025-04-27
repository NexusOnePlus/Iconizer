using Iconizer.Domain;

namespace Iconizer.Application;

public class IconProcessingService
{
    private readonly IIconService _iconService;

    public IconProcessingService(IIconService iconService)
    {
        _iconService = iconService;
    }

    public void ClearAllIcons(string folderPath)
    {
        _iconService.ClearIcons(folderPath);
    }
}
