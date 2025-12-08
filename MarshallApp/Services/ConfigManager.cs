using System.Diagnostics;
using MarshallApp.Models;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace MarshallApp.Services;

public static class ConfigManager
{
    private static AppConfig? _appConfig;
    private const string ConfigPath = "app_config.json";
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    
    private const int MaxRecent = 10;

    public static List<string> RecentProjects =>
        _appConfig?.RecentProjects ?? [];

    private static void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(ConfigPath, json);
    }

    private static async Task<AppConfig> Load()
    {
        if (!File.Exists(ConfigPath))
            return AppConfig.Default;

        try
        {
            var json = await File.ReadAllTextAsync(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? AppConfig.Default;
        }
        catch
        {
            return AppConfig.Default;
        }
    }
    
    public static void AddRecentProject(string path)
    {
        _appConfig ??= AppConfig.Default;

        if (string.IsNullOrWhiteSpace(path))
            return;

        path = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar);

        _appConfig.RecentProjects.Remove(path);
        _appConfig.RecentProjects.Insert(0, path);

        if (_appConfig.RecentProjects.Count > MaxRecent)
            _appConfig.RecentProjects.RemoveAt(_appConfig.RecentProjects.Count - 1);

        Save(_appConfig);
    }
    
    public static async void LoadAllConfigs()
    {
        try
        {
            _appConfig = await Load();

            var instance = MainWindow.Instance;
            if (instance != null)
            {
                instance.Width = _appConfig.WindowWidth;
                instance.Height = _appConfig.WindowHeight;
            }

            if (string.IsNullOrEmpty(_appConfig.LastProjectPath)) return;
            var project = ProjectManager.LoadProject(_appConfig.LastProjectPath);
            MainWindow.Instance?.CurrentProject = project;
            MainWindow.Instance?.SetProjectName(project.ProjectName);
            LoadBlocksFromProject(project);
            
            $"Project {instance?.CurrentProject?.ProjectName} has been opened.".Log();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public static void SaveAppConfig()
    {
        var instance = MainWindow.Instance;
        Debug.Assert(instance != null, nameof(instance) + " != null");

        _appConfig ??= AppConfig.Default;

        _appConfig.WindowWidth = instance.Width;
        _appConfig.WindowHeight = instance.Height;

        if (instance.CurrentProject != null)
        {
            _appConfig.LastProjectPath = Path.Combine(
                instance.CurrentProject.ProjectPath,
                instance.CurrentProject.ProjectName + ProjectManager.ProjectExtension
            );
        }

        SaveCurrentProject();

        $"Project {instance.CurrentProject?.ProjectName} has been saved to {_appConfig.LastProjectPath}".Log();
        
        Save(_appConfig);
    }

    private static void SaveCurrentProject()
    {
        var instance = MainWindow.Instance;
        Debug.Assert(instance != null, nameof(instance) + " != null");
        if (instance.CurrentProject == null) return;

        instance.CurrentProject.Blocks.Clear();
        foreach (var block in instance.Blocks)
        {
            instance.CurrentProject.Blocks.Add(new BlockConfig
            {
                PythonFilePath = block.FilePath,
                IsLooping = block.IsLooping,
                LoopIntervalSeconds = block.LoopInterval,
                OutputFontSize = block.OutputFontSize,
                X = Canvas.GetLeft(block),
                Y = Canvas.GetTop(block),
                WidthUnits = block.WidthUnits,
                HeightUnits = block.HeightUnits
            });
        }

        ProjectManager.SaveProject(instance.CurrentProject);
    }

    public static void LoadBlocksFromProject(Project project)
    {
        var instance = MainWindow.Instance;
        Debug.Assert(instance != null, nameof(instance) + " != null");
        
        instance.ClearBlocks();

        foreach (var cfg in project.Blocks)
        {
            var block = new BlockElement(instance.RemoveBlockElement, instance.LimitSettings)
            {
                FilePath = cfg.PythonFilePath,
                IsLooping = cfg.IsLooping,
                LoopInterval = cfg.LoopIntervalSeconds,
                OutputFontSize = cfg.OutputFontSize,
                WidthUnits = cfg.WidthUnits,
                HeightUnits = cfg.HeightUnits
            };

            block.Width = block.WidthUnits * BlockElement.GridSize;
            block.Height = block.HeightUnits * BlockElement.GridSize;

            instance.MainCanvas.Children.Add(block);
            instance.Blocks.Add(block);

            Canvas.SetLeft(block, cfg.X);
            Canvas.SetTop(block, cfg.Y);

            if (!string.IsNullOrEmpty(cfg.PythonFilePath))
                _ = block.RunPythonScript();
        }
    }
}
