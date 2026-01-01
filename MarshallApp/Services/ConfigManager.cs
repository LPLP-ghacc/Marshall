using System.Diagnostics;
using MarshallApp.Models;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace MarshallApp.Services;

public static class ConfigManager
{
    public static AppConfig? AppConfig;
    private const string ConfigPath = "app_config.json";
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    
    private const int MaxRecent = 10;

    public static List<string> RecentProjects =>
        AppConfig?.RecentProjects ?? [];

    private static async Task SaveAsync(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, Options);
        await File.WriteAllTextAsync(ConfigPath, json);
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
        catch (Exception ex)
        {
            ex.Message.Log();
            return AppConfig.Default;
        }
    }
    
    public static async Task AddRecentProjectAsync(string path)
    {
        AppConfig ??= AppConfig.Default;

        if (string.IsNullOrWhiteSpace(path))
            return;

        path = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar);

        AppConfig.RecentProjects.Remove(path);
        AppConfig.RecentProjects.Insert(0, path);

        if (AppConfig.RecentProjects.Count > MaxRecent)
            AppConfig.RecentProjects.RemoveAt(AppConfig.RecentProjects.Count - 1);

        await SaveAsync(AppConfig);
    }
    
    public static async Task LoadAllConfigs()
    {
        try
        {
            AppConfig = await Load();

            var instance = MainWindow.Instance;
            if (AppConfig.WindowWidth > 0) instance.Width = AppConfig.WindowWidth;
            if (AppConfig.WindowHeight > 0) instance.Height = AppConfig.WindowHeight;

            if (string.IsNullOrEmpty(AppConfig.LastProjectPath) || !File.Exists(AppConfig.LastProjectPath))
            {
                $"No last project to load. Path='{AppConfig.LastProjectPath}' Exists={File.Exists(AppConfig.LastProjectPath ?? "")}".Log();
                return;
            }
            var project = await ProjectManager.LoadProjectAsync(AppConfig.LastProjectPath);
            
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                MainWindow.Instance.CurrentProject = project;
                MainWindow.Instance.SetProjectName(project.ProjectName);
                LoadBlocksFromProject(project);
            });
            
            $"Project {instance.CurrentProject?.ProjectName} has been opened.".Log();
        }
        catch (Exception e)
        {
            e.Message.Log();
        }
    }
    
    public static async Task SaveAppConfigAsync()
    {
        var instance = MainWindow.Instance;
        
        AppConfig ??= AppConfig.Default;

        AppConfig.WindowWidth = instance.Width;
        AppConfig.WindowHeight = instance.Height;

        if (instance.CurrentProject != null)
        {
            AppConfig.LastProjectPath = Path.Combine(
                instance.CurrentProject.ProjectPath,
                instance.CurrentProject.ProjectName + ProjectManager.ProjectExtension
            );
        }

        await SaveCurrentProject();

        $"Project {instance.CurrentProject?.ProjectName} has been saved to {AppConfig.LastProjectPath}".Log();
        
        await SaveAsync(AppConfig);
    }

    private static async Task SaveCurrentProject()
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

        await ProjectManager.SaveProjectAsync(instance.CurrentProject);
    }

    public static void LoadBlocksFromProject(Project project)
    {
        var instance = MainWindow.Instance;
        Debug.Assert(instance != null, nameof(instance) + " != null");
        
        instance.ClearBlocks();
        
        "Start loading blocks.".Log();
        foreach (var cfg in project.Blocks)
        {
            $"{cfg.PythonFilePath}".Log();
            
            var block = new BlockElement(async void (block) =>
            {
                try
                {
                    await instance.RemoveBlockElement(block);
                }
                catch (Exception exception) { exception.Message.Log(); }
            }, instance.LimitSettings)
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
            
            block.RestoreLoopState();

            if (!string.IsNullOrEmpty(cfg.PythonFilePath))
                _ = block.RunPythonScript();
        }
    }
}
