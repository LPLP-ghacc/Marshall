namespace MarshallApp.Models;

public class Project(string projectName, string projectPath, List<BlockConfig> blocks)
{
    public string ProjectName { get; init; } = projectName;
    public string ProjectPath { get; init; } = projectPath;
    public List<BlockConfig> Blocks { get; init; } = blocks;
}