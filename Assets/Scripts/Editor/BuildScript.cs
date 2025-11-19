using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
public static class BuildScript
{
    private const string BuildPathArgument = "-buildPath";
    private const string BuildNameArgument = "-buildName";
    private const string BuildDevelopmentArgument = "-development";
    
    [MenuItem("Build/Build Server")]
    public static void BuildServer()
    {
        Debug.Log("Building server...");
        if (!TryGetArgumentValue(BuildNameArgument, out var buildName))
        {
            Debug.LogError("Build name not specified");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"Build name: {buildName}");
        Build(BuildTarget.StandaloneLinux64, 1, buildName);
    }

    [MenuItem("Build/Build Web Client")]
    public static void BuildWebClient()
    {
        Debug.Log("Building web client...");
        Build(BuildTarget.WebGL);
    }

    private static void Build(BuildTarget target, int subtarget = 0, string buildName = "")
    {
        if (!TryGetArgumentValue(BuildPathArgument, out string buildPath))
        {
            Debug.LogError("Build path not specified");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"Build path: {buildPath}");
        if (Directory.Exists(buildPath))
        {
            Debug.Log("Deleting old build...");
            Directory.Delete(buildPath, true);
        }

        var buildOptions = BuildOptions.CleanBuildCache;
        if (HasArgument(BuildDevelopmentArgument))
            buildOptions |= BuildOptions.Development;
        
        string resultPath = Path.Combine(buildPath, buildName);
        var options = new BuildPlayerOptions
        {
            scenes = GetBuildScenes(),
            locationPathName = resultPath,
            target = target,
            subtarget = subtarget,
            options = buildOptions,
        };
        
        Debug.Log($"Executing build at {resultPath}");
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log("Build succeeded");
        EditorApplication.Exit(0);
    }

    private static string[] GetBuildScenes()
    {
        var scenes = new string[SceneManager.sceneCountInBuildSettings];
        for (var i = 0; i < scenes.Length; i++)
        {
            scenes[i] = SceneUtility.GetScenePathByBuildIndex(i);
        }

        return scenes;
    }
    
    private static bool TryGetArgumentValue(string argumentName, out string value)
    {
        Debug.Log($"Searching for argument: {argumentName}");
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], argumentName) && i + 1 < args.Length)
            {
                value = args[i + 1];
                Debug.Log($"Found argument {argumentName} value: {value}");
                return true;
            }
        }

        Debug.Log($"Argument {argumentName} not found");
        value = null;
        return false;
    }
    
    private static bool HasArgument(string argumentName)
    {
        Debug.Log($"Searching for argument: {argumentName}");
        string[] args = Environment.GetCommandLineArgs();
        foreach (var argument in args)
        {
            if (string.Equals(argument, argumentName))
            {
                Debug.Log($"Found argument {argumentName}");
                return true;
            }
        }

        Debug.Log($"Argument {argumentName} not found");
        return false;
    }
}