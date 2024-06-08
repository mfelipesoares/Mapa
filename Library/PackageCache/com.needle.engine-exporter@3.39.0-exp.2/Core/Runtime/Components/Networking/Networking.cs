using System.Text.RegularExpressions;
using UnityEngine;

namespace Needle.Engine.Components
{
    [AddComponentMenu("Needle Engine/Networking/Networking" + Needle.Engine.Constants.NeedleComponentTags)]
    [HelpURL(Constants.DocumentationUrlNetworking)]
    public class Networking : MonoBehaviour
    {
	    [Info("This component is used to override the networking backend url that is currently built in the core runtime package. Add a websocket url below to connect to a different backend:")]
        [Tooltip(nameof(url) + " can be a absolute url starting with https:// or http:// or wss:// or ws:// or be just a relative url starting with /.\n" +
              "When using a relative url we expect the same path to exist on the hosted website and during local development the field localhost below will be used.")]
        public string url = "/socket";
        [Tooltip("Used to allow to specify the backend networking url as a url parameter. E.g. ?server=https://mynetworkingbackend.com - leave empty if you dont want to allow that")]
        public string urlParameterName;
        
        [Header("Use for local dev"), Tooltip("Only used when url above is a relative url")]
        public string localhost = "needle-tiny-starter.glitch.me";

        private void OnValidate()
        {
            if (localhost != null)
            {
                var suc = glitchRegex.Match(localhost);
                if (suc.Success)
                {
                    localhost = "https://" + suc.Groups["project_name"].Value + ".glitch.me";
                }
            }
        }

        // https://regex101.com/r/bbhdSr/1
        private static readonly Regex glitchRegex = new Regex("glitch.com\\/.+\\/(?<project_name>[A-Za-z\\-]+)(\\?.+)?$", RegexOptions.Compiled);
        
        #if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(Networking))]
        internal class NetworkingEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                GUILayout.Space(5);
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Local Networking Example " + Constants.ExternalLinkChar))
                    {
                        Application.OpenURL("https://fwd.needle.tools/needle-engine/local-networking-repository");
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }
        #endif
    }
}
