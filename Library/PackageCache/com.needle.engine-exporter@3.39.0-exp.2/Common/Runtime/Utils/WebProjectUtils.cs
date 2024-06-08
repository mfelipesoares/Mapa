using System.Threading.Tasks;
using UnityEngine;

namespace Needle.Engine.Utils
{
    internal static class WebProjectUtils
    {
        /// <summary>
        /// Open the preview server. This assumes that the project has been built before calling this command
        /// </summary>
        public static async Task<bool> StartPreviewServer(string projectDirectory)
        {
            var packageJsonPath = projectDirectory + "/package.json";
            if (PackageUtils.TryGetScripts(packageJsonPath, out var scripts))
            {
                var addedPreviewScript = false;
                if (!scripts.ContainsKey("preview"))
                {
                    addedPreviewScript = true;
                    if (!InsertPreviewScript())
                    {
                        Debug.LogError(
                            "Could not insert preview script: Please insert a new script manually in your package.json: \"preview\" : \"<your_command_to_start_preview>\"\nAt " +
                            packageJsonPath.AsLink());
                        return false;
                    }

                    if (!PackageUtils.TryWriteScripts(packageJsonPath, scripts))
                    {
                        Debug.LogWarning(
                            "Missing preview script in package.json. Add \"preview\" : \"vite preview\" to your package.json at " +
                            packageJsonPath.AsLink());
                        return false;
                    }

                    Debug.LogWarning(
                        "Added temporary preview script: \"preview\" because no preview script was found in package.json at " +
                        packageJsonPath + "\n\"preview\": \"" + scripts["preview"] + "\"");
                }

                var preview = ProcessHelper.RunCommand("npm run preview", projectDirectory,
                    null, false, true);

                if (addedPreviewScript)
                {
                    await Task.Delay(5000);
                    scripts.Remove("preview");
                    PackageUtils.TryWriteScripts(packageJsonPath, scripts);
                }

                return await preview;
            }

            Debug.LogError("Could not find package.json in " + projectDirectory + " to start preview server");
            return false;

            bool InsertPreviewScript()
            {
                foreach (var script in scripts)
                {
                    if (script.Value.Contains("vite"))
                    {
                        // https://vitejs.dev/guide/cli.html#vite-preview
                        scripts.Add("preview", "vite preview --host --port 3300 --open");
                        return true;
                    }

                    if (script.Value.Contains("next"))
                    {
                        // https://github.com/vercel/next.js/discussions/13448#discussioncomment-6491542
                        // https://nextjs.org/docs/pages/api-reference/next-cli#production
                        var openBrowserCommand = "URL=http://localhost:3300 && (open $URL || cmd.exe /c start $URL)";
                        scripts.Add("preview", $"{openBrowserCommand} && next start -p 3300 ");
                        return true;
                    }
                }

                return false;
            }
        }
    }
}