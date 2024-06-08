using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Needle.Engine.Samples.Helpers
{
    class SerializerSettings
    {
        public static JsonSerializerSettings Get(string exportRoot = null)
        {
            var allTags = AssetDatabase.FindAssets("t:Tag")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Tag>)
                .Where(x => x)
                .ToArray();
            
            var converters = new List<JsonConverter>()
            {
                new Texture2DConverter(),
                new TagConverter(allTags),
                new ScriptableObjectConverter<SampleInfo>(),
                new ScriptableObjectConverter<SampleCollection>(),
            };
            
            if (exportRoot != null)
                Texture2DConverter.RootPath = exportRoot;

            var resolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            resolver.IgnoreProperty(typeof(Object), nameof(Object.name));
            resolver.IgnoreProperty(typeof(Object), nameof(Object.hideFlags));
            resolver.IgnoreProperty(typeof(Vector2), nameof(Vector2.normalized));
            resolver.IgnoreProperty(typeof(Vector2), nameof(Vector2.magnitude));
            resolver.IgnoreProperty(typeof(Vector2), nameof(Vector2.sqrMagnitude));
            resolver.IgnoreProperty(typeof(Vector3), nameof(Vector3.normalized));
            resolver.IgnoreProperty(typeof(Vector3), nameof(Vector3.magnitude));
            resolver.IgnoreProperty(typeof(Vector3), nameof(Vector3.sqrMagnitude));
            resolver.IgnoreProperty(typeof(Quaternion), nameof(Quaternion.eulerAngles));
            resolver.IgnoreProperty(typeof(Color), nameof(Color.linear));
            resolver.IgnoreProperty(typeof(Color), nameof(Color.gamma));
            resolver.IgnoreProperty(typeof(Color), nameof(Color.grayscale));
            resolver.IgnoreProperty(typeof(Color), nameof(Color.maxColorComponent));
            resolver.IgnoreAllObsolete(typeof(Component));
            resolver.IgnoreProperty(typeof(MonoBehaviour), nameof(MonoBehaviour.useGUILayout));
            resolver.IgnoreProperty(typeof(Behaviour), nameof(Behaviour.isActiveAndEnabled));
            resolver.IgnoreProperty(typeof(MonoBehaviour), "runInEditMode");

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = converters,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
                ContractResolver = resolver,
            };
            
            return settings;
        }
    }

    class TagConverter : JsonConverter<Tag>
    {
        private readonly Dictionary<string, Tag> _allTags;
        public TagConverter(Tag[] allTags)
        {
            _allTags = new Dictionary<string, Tag>();
            foreach (var tag in allTags)
            {
                if (!tag || string.IsNullOrEmpty(tag.name)) continue;
                if (_allTags.ContainsKey(tag.name)) continue;
                _allTags.Add(tag.name, tag);
            }
        }

        public override void WriteJson(JsonWriter writer, Tag value, JsonSerializer serializer)
        {
            writer.WriteValue(value.name);
        }

        public override Tag ReadJson(JsonReader reader, Type objectType, Tag existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var val = reader.Value as string;
            if (existingValue) return existingValue;
            if (val == null) return null;
            if (!_allTags.ContainsKey(val))
            {
                var t = ScriptableObject.CreateInstance<Tag>();
                t.name = val;
                _allTags.Add(val, t);
                return t;
            }
            return _allTags[val];
        }
    }

    class Texture2DConverter : JsonConverter<Texture2D>
    {
        internal static string RootPath = "../../";
        
        public static string GetPathForTexture(Texture texture)
        {
            if (!texture) return null;
            var path = Path.GetFullPath(AssetDatabase.GetAssetPath(texture));
            var fullRootPath = Path.GetFullPath(RootPath);
            var relativePath = path.Replace(fullRootPath, "").Replace("\\","/").Replace("\\","/");
            return relativePath;
        }
        
        public override void WriteJson(JsonWriter writer, Texture2D value, JsonSerializer serializer)
        {
            if (!value)
            {
                writer.WriteNull();
                return;
            }
            
            serializer.Serialize(writer, new TextureData()
            {
                relativePath = GetPathForTexture(value),
                absolutePath = Constants.RepoRoot + GetPathForTexture(value) + "?v=" + value.imageContentsHash,
            });
        }

        private struct TextureData
        {
            public string relativePath;
            public string absolutePath;
        }
        
        public override Texture2D ReadJson(JsonReader reader, Type objectType, Texture2D existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var t = existingValue;
            var textureData = serializer.Deserialize<TextureData>(reader);
            if (string.IsNullOrEmpty(textureData.absolutePath))
            {
                return null;
            }
            
            if (!hasExistingValue)
            {
                if (TextureCache.TryGetValue(textureData.absolutePath, out t) && t)
                {
                    return t;
                }
                
                t = new Texture2D(1, 1);
                t.SetPixels(new Color[] { Color.clear });
            }
            var str = textureData.relativePath;
            DownloadAndCacheImageAndFillTexture(t, textureData, serializer.Context.Context);
            
            t.name = string.IsNullOrEmpty(str) ? "no texture" : str;
            return t;
        }

        // cache for this session
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private async void DownloadAndCacheImageAndFillTexture(Texture2D target, TextureData textureData, object context)
        {
            var path = textureData.relativePath;
            
            if (path.StartsWith("assets")) 
                path = path.Replace("assets/", "Packages/com.needle.engine-samples/");
                
            if (!File.Exists(path))
                path = textureData.absolutePath;

            var cachePath = Constants.CacheRoot + SamplesWindow.SanitizePath(Path.GetFileName(path));

            if (File.Exists(cachePath))
                path = cachePath;
            
            // use file:// for local files
            if (File.Exists(path))
                path = "file://" + Path.GetFullPath(path);

            var request = new UnityWebRequest(path);
            request.downloadHandler = new DownloadHandlerBuffer();
#if UNITY_2023_3_OR_NEWER
            await request.SendWebRequest();
#else
            request.SendWebRequest();
#endif
            while (!request.isDone)
                await Task.Yield();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var data = request.downloadHandler.data;
                target.LoadImage(data, true);
                
                if (context is EditorWindow editorWindow) SamplesWindow.MarkImagesDirty(editorWindow.rootVisualElement);
                if (context is Editor editor) editor.Repaint();
                
                if (!path.StartsWith("file://"))
                {
                    Directory.CreateDirectory(Constants.CacheRoot);
                    File.WriteAllBytes(cachePath, data);
                    
                    // cache for this session
                    TextureCache[textureData.absolutePath] = target;
                }
            }
            else Debug.Log(request.error);
        }
    }

    class ScriptableObjectConverter<T> : CustomCreationConverter<T> where T : ScriptableObject
    {
        public override T Create(Type objectType)
        {
            return ScriptableObject.CreateInstance<T>();
        }
    }
}