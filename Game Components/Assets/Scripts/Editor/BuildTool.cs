using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Editor tool behind Tools, Grappler, Build Windows Game. It makes the
/// standalone Windows version of the game (NFR6) in the Builds/Windows folder
/// next to Assets, so anyone on the team gets the same build with one click.
///
/// It builds every scene that's turned on in the build list, starting with
/// the main menu. It's a normal release build, so the F1 to F5 testing
/// shortcut is turned off, but the F8 frame rate counter still works for
/// checking NFR2 on the lab computers. The Builds folder is in .gitignore, so
/// the built game doesn't get committed to the repo.
/// </summary>
public static class BuildTool
{
    private const string OutputFolder = "Builds/Windows";
    private const string ExeName = "GrapplerGame.exe";

    /// <summary>Builds the Windows game and opens the folder it's in when it's done.</summary>
    [MenuItem("Tools/Grappler/Build Windows Game", priority = 70)]
    public static void BuildWindows()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Grappler", "No scenes are in the build list. Run Build Main Menu Scene first.", "OK");
            return;
        }

        Directory.CreateDirectory(OutputFolder);
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(OutputFolder, ExeName),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Grappler: Windows build finished in {summary.totalTime.TotalSeconds:0} seconds " +
                      $"({summary.totalSize / (1024 * 1024)} MB) at {Path.GetFullPath(OutputFolder)}.");
            EditorUtility.RevealInFinder(Path.Combine(OutputFolder, ExeName));
        }
        else
        {
            Debug.LogError($"Grappler: Windows build {summary.result} with {summary.totalErrors} errors. Check the Console.");
        }
    }
}
