using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Codegen
{
	public class ComponentGeneratorRunner
	{
		public bool debug = false;
		
		private static DateTime lastNodeJsWarningLogTime = DateTime.MinValue;
		
		public async Task<bool> Run(string generatorInstallDir, string filePath, string targetDirectory, string seed = null)
		{
#if UNITY_EDITOR
			if (string.IsNullOrWhiteSpace(generatorInstallDir) || !Directory.Exists(generatorInstallDir))
			{
				Debug.LogError("Can not generate components: component compiler is not found");
				return false;
			}
			if (filePath == null || (!filePath.EndsWith(".ts") && !filePath.EndsWith(".js"))) return false;
			if (filePath.EndsWith(".d.ts")) return false;
			if (!File.Exists(filePath)) return false; 
			
			var typesInFile = new List<ImportInfo>();
			TypeScanner.FindTypesInFile(filePath, typesInFile);
			var expectedScriptTypesThatDontExistYet = new List<string>();
			
			foreach (var i in typesInFile)
			{
				var scriptPath = i.TypeName + ".cs";
				var expectedGuid = ComponentGeneratorUtil.GetGuid(scriptPath, seed);
				var foundAssetPath = AssetDatabase.GUIDToAssetPath(expectedGuid);
				
				NeedleDebug.Log(TracingScenario.ComponentGeneration, "Searching for " + scriptPath + " with GUID " + expectedGuid + ", at " + foundAssetPath + "; seed: " + seed);
				
				// if any GUID already exists in the AssetDatabase we will use that folder as targetDirectory
				if (!string.IsNullOrEmpty(foundAssetPath))
				{
					targetDirectory = Path.GetDirectoryName(foundAssetPath);
					NeedleDebug.Log(TracingScenario.ComponentGeneration, 
						$"Found existing GUID for {i.TypeName} from {Path.GetFileName(filePath)} at {targetDirectory}. Generating in that folder.");
					break;
				}
			}

			foreach (var i in typesInFile)
			{
				var scriptPath = targetDirectory + "/" + i.TypeName + ".cs";
				if (!File.Exists(scriptPath))
					expectedScriptTypesThatDontExistYet.Add(scriptPath);
			}
			
			var logPath = $"{Application.dataPath}/../Temp/component-compiler.log";

			// this is set by the ExportInfo component - if it's set to 1 then we know that node is installed
			if (SessionState.GetInt("NEEDLE_NODE_INSTALLED", -1) != 1)
			{
				if (debug) Debug.Log("Checking if Nodejs is installed...");
				if (!await ProcessHelper.RunCommand("node --version", null, logPath, true, debug))
				{
					var timeSinceLastLog = DateTime.Now - lastNodeJsWarningLogTime;
					if (timeSinceLastLog.TotalSeconds > 5)
					{
						lastNodeJsWarningLogTime = DateTime.Now;
						Debug.LogWarning("Detected script change but Node.js is not installed or could not be found. Please install Node.js to enable automatic C# code generation.\n\nLogs: " + logPath.AsLink());
					}
					return false;
				}
			}
			
			if (debug) Debug.Log("Node.js is installed: invoke component compiler...");

			targetDirectory = Path.GetFullPath(targetDirectory);
			if(targetDirectory.EndsWith("\\")) targetDirectory = targetDirectory.Substring(0, targetDirectory.Length - 1);
			if(targetDirectory.EndsWith("/")) targetDirectory = targetDirectory.Substring(0, targetDirectory.Length - 1);
			var cmd = $"node component-compiler.js \"{targetDirectory}\"";
			cmd += " \"" + Path.GetFullPath(filePath) + "\"";
			
			var logLink = $"<a href=\"{logPath}\">{logPath}</a>";
			var workingDir = Path.GetFullPath(generatorInstallDir);

			TypesGenerator.GenerateTypesIfNecessary();
			var typesFilePath = TypesGenerator.CodeGenTypesFile;
			if (File.Exists(typesFilePath)) File.Copy(typesFilePath, workingDir + "/types.json", true);
			
			Debug.Log( $"<b>Run codegen</b> for {Path.GetFileName(filePath)} at <a href=\"{filePath}\">{filePath}</a>\n\n<b>Command</b>: <i>{cmd}</i>\n\n<b>Directory</b>: {workingDir}\n\n<b>Log at</b>: {logLink}");
			Directory.CreateDirectory(targetDirectory);
			
			ComponentGeneratorUtil.EnsureAssemblyDefinition(targetDirectory);

			if (debug) cmd += " & pause";
			
			if (await ProcessHelper.RunCommand(cmd, workingDir, logPath, !debug, debug))
			{
				TypesUtils.MarkDirty();
				AssetDatabase.Refresh();
				foreach (var exp in expectedScriptTypesThatDontExistYet)
				{
					// the script didnt exist before but does now
					if (File.Exists(exp))
					{
						var guid = ComponentGeneratorUtil.GenerateAndSetStableGuid(exp, seed);
						if (!string.IsNullOrEmpty(guid))
						{
							LogDelayed(exp, guid);
						}
					}
				}
				
				// for checking if expected GUID matches the current one
				/*
				foreach (var i in typesInFile)
				{
					var exp = targetDirectory + "/" + i.TypeName + ".cs";
					var newGuid = ComponentGeneratorUtil.GetGuid(exp, seed);
					Debug.Log(exp + " => " + newGuid + "; seed: " + seed);
				}
				*/
				
				await Task.Delay(100);
				AssetDatabase.Refresh();
				return true;
			}
			
			Debug.LogWarning("Compilation failed, see log for more info: " + logLink);
#else
			await Task.CompletedTask;
#endif
			return false;
		}

		private static async void LogDelayed(string path, string guid)
		{
			await Task.Delay(1000);
#if UNITY_EDITOR
			var relative = PathUtils.MakeProjectRelative(path);
			Debug.Log(Path.GetFileName(path) + " at " + path + "\n" + guid,
				AssetDatabase.LoadAssetAtPath<Object>(relative));
#endif
		}
	}
}