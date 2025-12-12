using System.IO;
using System.Text.Json;
using MarshallApp.Models;

namespace MarshallApp.Services;

public abstract class ProjectManager
{
    public const string ProjectExtension = ".mpr";
    
    public static void SaveProject(Project project)
    {
        var filePath = Path.Combine(project.ProjectPath, project.ProjectName + ProjectExtension);

        var json = JsonSerializer.Serialize(project, ConfigManager.Options);
        File.WriteAllText(filePath, json);
    }

    public static Project LoadProject(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Project>(json)
               ?? throw new Exception("Invalid project file.");
    }

    public static Project CreateNewProject(string folder, string name)
    {
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "Scripts"));

        var project = new Project(name, folder, []);

        SaveProject(project);

        return project;
    }
}