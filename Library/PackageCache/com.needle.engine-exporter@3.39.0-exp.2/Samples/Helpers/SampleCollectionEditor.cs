using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Needle.Engine.Samples.Helpers
{
    [CustomEditor(typeof(SampleCollection))]
    internal class SampleCollectionEditor : Editor
    {
        private VisualElement root;
        private VisualElement devTools;
        
        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            root.Add(new Button(() =>
            {
                EditorWindow.GetWindow<SamplesWindow>().Show();
            }) { text = "Open As Window" });
            var v = new VisualElement();
            root.Add(v);
            var activeInspector = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(x => x.GetType().Name == "InspectorWindow");
            SamplesWindow.RefreshAndCreateSampleView(v, activeInspector);

            AddOrRemoveDevTools();
            return root;
        }

        [MenuItem("CONTEXT/" + nameof(SampleCollection) + "/Toggle DevTools")]
        private static void ToggleDevTools(MenuCommand item)
        {
            var obj = Resources.FindObjectsOfTypeAll<SampleCollectionEditor>();
            foreach(var ed in obj) ed.AddOrRemoveDevTools();
        }

        private async void AddOrRemoveDevTools()
        {
            if (devTools != null)
            {
                if (devTools.parent != null)
                {
                    SetDevMode(false);
                    devTools.RemoveFromHierarchy();
                }
                else
                {
                    SetDevMode(true);
                    root.Insert(0, devTools);
                }
                return;
            }
            
            devTools = new VisualElement();
            devTools.Add(new Button(() =>
            {
                ProduceSampleArtifacts();
            }) { text = "Update Samples Artifacts", tooltip = "Creates samples.json and Samples.md in the repo root with the current sample data.\nAlso bumps the Needle Exporter dependency in the samples package to the current."});
            devTools.Add(new Button(() =>
            {
                ExportLocalPackage();
            }) { text = "Export Local Package .tgz", tooltip = "Outputs the Samples package as immutable needle-engine-samples.tgz.\nThis is referenced by Tests projects to get the same experience as installing the package from a registry." });

            var spacer = new VisualElement();
            spacer.style.height = 10;
            devTools.Add(spacer);
            
            // check if we're in dev mode
            // var path = Path.GetFullPath("Assets");
            // var parentDir = Path.GetDirectoryName(Path.GetDirectoryName(path))!.Replace("\\", "/");
            if (IsInDevMode)
            {
                await Task.Delay(10);
                root.Insert(0, devTools);
            }
        }

        private static bool IsInDevMode => PackageUtils.IsMutable(Engine.Constants.TestPackagePath) ||
                                    SessionState.GetBool("Needle_SamplesWindow_DevMode", false);
        private static bool SetDevMode(bool value)
        {
            SessionState.SetBool("Needle_SamplesWindow_DevMode", value);
            return value;
        }

        private static bool SamplesPackageIsMutable => PackageUtils.IsMutable("Packages/" + Constants.SamplesPackageName); 
        
        internal static async void ProduceSampleArtifacts(List<SampleInfo> sampleInfos = null)
        {
            var rootPath = GetRootPath();
            
            sampleInfos ??= SamplesWindow.GetLocalSampleInfos();
            
            // produce JSON
            var jsonPath = rootPath + "/samples.json";
            var readmePath = rootPath + "/Samples.md";
            var sc = CreateInstance<SampleCollection>();
            sc.samples = sampleInfos;
            var serializerSettings = SerializerSettings.Get(rootPath);
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(sc, Formatting.Indented, serializerSettings));
            Debug.Log("Write samples.json " + jsonPath);
            
            // produce markdown
            var readme = new List<string>();
            readme.Add("# Samples");
            readme.Add("");
            readme.Add("This is a list of all samples in this package. You can also find them in the Unity Package Manager window.");
            readme.Add("");
            readme.Add("## Samples");
            readme.Add("");
            readme.Add("| Sample | Description | Preview | ");
            readme.Add("| --- | --- | --- |");
            foreach (var info in sampleInfos)
            {
                readme.Add(
                    $"| {(string.IsNullOrEmpty(info.LiveUrl) ? info.DisplayNameOrName : $"[{info.DisplayNameOrName}]({info.LiveUrl})")} " +
                    $"| {info.Description} " +
                    $"<br/>{(info.Tags != null ? string.Join(" ", info.Tags.Select(x => "<kbd>" + x.name + "</kbd>")) : "")}" +
                    $"| {(info.Thumbnail ? $"<img src=\"{Texture2DConverter.GetPathForTexture(info.Thumbnail)}\" height=\"200\"/>" : "")}"
                );

            }
            readme.Add("");
            File.WriteAllLines(readmePath, readme);
            Debug.Log("Write readme " + readmePath);
            
            // bump dependency to Needle Engine with the one currently set
            var packageJsonPath = "Packages/" + Constants.SamplesPackageName + "/package.json";
            var packageJson = File.ReadAllText(packageJsonPath);
            var allPackages = Client.Search(Constants.ExporterPackageName, true);
            while (!allPackages.IsCompleted)
                await Task.Yield();
            var result = allPackages.Result.FirstOrDefault();
            if (result != null)
            {
                var pattern = $"(\"{Constants.ExporterPackageName}\") *: *\"(.*)\"";
                packageJson = Regex.Replace(packageJson, pattern, $"$1: \"{result.versions.latestCompatible}\"");
                File.WriteAllText(packageJsonPath, packageJson);
                Debug.Log("Update Needle Engine dependency " + packageJsonPath + " to " + result.versions.latestCompatible);
            }
        }

        internal static async void ExportLocalPackage()
        {
            var rootPath = GetRootPath();
            var packageFolder = Path.GetFullPath("Packages/" + Constants.SamplesPackageName);
            var targetFolder = Path.GetFullPath(rootPath);
            Debug.Log("Packing " + packageFolder + " → " + targetFolder);
            var packRequest = Client.Pack(packageFolder, targetFolder);
            while (!packRequest.IsCompleted)
                await Task.Yield();
            if (packRequest.Status == StatusCode.Success)
            {
                var tarball = packRequest.Result.tarballPath;
                var target = Path.GetDirectoryName(tarball) + "/needle-engine-samples.tgz";
                if (File.Exists(target)) File.Delete(target);
                File.Move(tarball, target);
                var fileSize = File.Exists(target) ? new FileInfo(target).Length : 0;
                var fileSizeMb = $"{fileSize / 1024f / 1024f:F1} MB";
                Debug.Log($"Success → {target} ({fileSizeMb})");
                EditorUtility.RevealInFinder(target); 
            }
        }

        private static string GetRootPath()
        {
            if (!SamplesPackageIsMutable)
            {
                return Path.GetFullPath(Application.dataPath + "/../Temp/Needle");
            }

            return Path.GetFullPath("Packages/" + Constants.SamplesPackageName) + "/../";
        }
    }
}
