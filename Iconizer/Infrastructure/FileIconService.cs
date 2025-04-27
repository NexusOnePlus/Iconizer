using Iconizer.Domain;

namespace Iconizer.Infrastructure;

public class FileIconService : IIconService
{
    public List<IconItem> GetIcons(string folderPath)
    {
        // Read icons from folder
        return null;
    }

    public void ClearIcons(string folderPath)
    {
        // Delete or clean icons
    }
}
