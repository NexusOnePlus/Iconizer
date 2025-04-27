namespace Iconizer.Domain;

public interface IIconService
{
    List<IconItem> GetIcons(string folderPath);
    void ClearIcons(string folderPath);
}
