using System.IO;
using System.Text.Json;
using MarshallApp.Models;

namespace MarshallApp.Services;

public abstract class ProjectManager
{
    public const string ProjectExtension = ".mpr";
    
    public static async Task SaveProjectAsync(Project project)
    {
        var filePath = Path.Combine(project.ProjectPath, project.ProjectName + ProjectExtension);

        var json = JsonSerializer.Serialize(project, ConfigManager.Options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task<Project> LoadProjectAsync(string filePath)
    {
        $"Trying to load project {filePath}".Log();
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Project>(json)
               ?? throw new Exception("Invalid project file.");
    }

    public static Project CreateNewProject(string folder, string name)
    {
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "Scripts"));

        var project = new Project(name, folder, []);

        SaveProjectAsync(project);
        $"Created and saved {project.ProjectName}".Log();
        return project;
    }
}