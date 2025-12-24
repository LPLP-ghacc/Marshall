using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MarshallApp.Controllers;

public class WallpaperController
{
    public readonly string WorkingDirectory;
    private readonly ImageBrush _brush;
    private readonly List<string> _imageSet;
    
    public WallpaperController(ImageBrush brush, string  workingDirectory)
    {
        _brush =  brush;
        WorkingDirectory = workingDirectory;
        
        _imageSet = GetImages();
    }

    public void Update()
    {
        if (_imageSet.Count <= 0) return;
        var rnd = new Random();
        var current = rnd.Next(0, _imageSet.Count);
        _brush.ImageSource = new BitmapImage(new Uri(Path.Combine(WorkingDirectory, _imageSet[current])));
    }

    private List<string> GetImages()
    {
        var imageSet = new List<string>();
        var files = Directory.GetFiles(WorkingDirectory);

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file);
            if (extension is not (".jpg" or ".jpeg" or ".png")) continue;
            imageSet.Add(file);
        }
        
        return imageSet;
    }
}