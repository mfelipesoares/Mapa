using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
    internal class HiddenProject
    {
        private static readonly string InstallDirectory =
            Path.GetDirectoryName(Application.dataPath).Replace("\\", "/") + "/Temp/@needle-tools-npm-tools";

        private static readonly string PackageJsonPath = InstallDirectory + "/package.json";

        internal static string BuildPipelinePath { get; } =
            $"{InstallDirectory}/node_modules/{Constants.GltfBuildPipelineNpmPackageName}";

        internal static string ComponentCompilerPath { get; } =
            $"{InstallDirectory}/node_modules/{Constants.ComponentCompilerNpmPackageName}";

        internal static string ToolsPath { get; } =
            $"{InstallDirectory}/node_modules/{Constants.ToolsNpmPackageName}";


#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static async void Init()
        {
            while (EditorApplication.isCompiling || EditorApplication.isUpdating) await Task.Delay(1000);
            await Task.Delay(1000);
            await Initialize(true);
        }
#endif

        internal static void Delete()
        {
            try
            {
                if (Directory.Exists(InstallDirectory))
                {
                    Directory.Delete(InstallDirectory, true);
                }
            }
            catch (Exception e)
            {
                NeedleDebug.LogException(TracingScenario.Any, e);
            }
        }
        
        internal static Task<bool> Initialize(bool silent = false)
        {
#if UNITY_EDITOR
            if (File.Exists(InstallDirectory + "/node_modules/.package-lock.json"))
            {
                // on the main thread we can check if the didInitialize bool is true
                if (UnityThreads.IsMainThread())
                {
                    if (didInitialize) return Task.FromResult(true);
                }                
                // if we have any files in the node_modules directory then we assume that the installation was successful
                else if (Directory.EnumerateDirectories(InstallDirectory).Any()) return Task.FromResult(true);
            }

            if (initializationTask != null)
            {
                NeedleDebug.Log(TracingScenario.NetworkRequests, "Initialization task... Status=" + initializationTask.Status);
                if (initializationTask.Status == TaskStatus.RanToCompletion && initializationTask.Result && File.Exists(InstallDirectory + "/node_modules/.package-lock.json"))
                    return initializationTask;
                if (initializationTask.Status != TaskStatus.RanToCompletion)
                    return initializationTask;
            }

            if (!silent)
            {
                Debug.Log("Initializing Needle Engine Tools...".LowContrast());
            }

            try
            {
                var t = CreateToolsPackage().ContinueWith(r =>
                {
                    didInitialize = r.Result;
                    return r.Result;
                }, TaskScheduler.FromCurrentSynchronizationContext());
                initializationTask = t;
                return t;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to initialize Needle Engine Tools: " + ex);
                if (EditorUtility.DisplayDialog("Tools Initialization Failed",
                        "Needle Engine Tools failed to initialize because of \"" + ex.Message +
                        "\"\n\nDo you want to try again?", "Yes try again", "No"))
                {
                    return Initialize(silent);
                }
                return Task.FromResult(false);
            }
#else
			return Task.FromResult(false);
#endif
        }

#if UNITY_EDITOR
        private static bool didInitialize
        {
            get => SessionState.GetBool("NPMToolsDidInitialize", false);
            set => SessionState.SetBool("NPMToolsDidInitialize", value);
        }

        private static Task<bool> initializationTask;

        private static async Task<bool> CreateToolsPackage(int iteration = 0)
        {
            Directory.CreateDirectory(InstallDirectory);
            if (!File.Exists(PackageJsonPath))
            {
                File.WriteAllText(PackageJsonPath, "{}");
            }

            var logFilePath = InstallDirectory + "/npm.log";
            var json = File.ReadAllText(PackageJsonPath);
            var obj = JObject.Parse(json);
            obj["name"] = "@needle-tools/editor-tools";
            obj["version"] = ProjectInfo.GetCurrentPackageVersion(Constants.UnityPackageName, out _) ?? "1.0.0";
            obj["description"] =
                $"Npm Tools generated by {Constants.UnityPackageName}@{ProjectInfo.GetCurrentPackageVersion(Constants.UnityPackageName, out _)}";
            var deps = new JObject();
            obj["dependencies"] = deps;
            AddDependency(Constants.GltfBuildPipelineNpmPackageName, deps);
            AddDependency(Constants.ToolsNpmPackageName, deps);
            AddDependency(Constants.ComponentCompilerNpmPackageName, deps, "^1.0.0-pre");
            File.WriteAllText(PackageJsonPath, obj.ToString());
            var lockPath = InstallDirectory + "/package-lock.json";
            if (File.Exists(lockPath)) File.Delete(lockPath);
            // Ignore engines because of the sharp dependency for NODE < 18.17
            var cmd = "npm set registry https://registry.npmjs.org && npm update && " + NpmUtils.GetInstallCommand(InstallDirectory) + " --ignore-engines";
            var res = await ProcessHelper.RunCommand(cmd, InstallDirectory, logFilePath, true, false, -1);
            // if this fails for some reason we try it again once more
            if(!res && iteration < 1)
            {
                await Task.Delay(1000);
                await FileUtils.DeleteDirectoryRecursive(InstallDirectory);
                return await CreateToolsPackage(iteration + 1);
            }
            return res;
        }

        private static void AddDependency(string packageName, JObject deps, string defaultVersion = "latest")
        {
            deps.Add(packageName, NpmUnityEditorVersions.TryGetRecommendedVersion(packageName, defaultVersion));
        }


#endif
    }
}