using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Needle.Engine.Utils;
using UnityEditor;
#endif

namespace Needle.Engine
{
	[Flags]
	internal enum TracingScenario
	{
		Any = 0,
		Types = 1 << 0,
		NetworkRequests = 1 << 1,
		ComponentGeneration = 1 << 2,
		ColorSpaces = 1 << 3,
		FileExport = 1 << 4,
		EditorSync = 1 << 5,
		Samples = 1 << 6,
		/// <summary>
		/// E.g. Tools package or BuildPipeline package logs
		/// </summary>
		Tools = 1 << 7,
	}

	internal static class NeedleDebug
	{
#if HAS_HIDE_IN_CALLSTACKS
		[HideInCallstack]
#endif
		public static void Log(TracingScenario scenario, object obj, Object context = null)
		{
			if (IsEnabled(scenario)) Debug.Log(obj, context);
		}
		public static void Log(TracingScenario scenario, Func<object> obj, Object context = null)
		{
			if (IsEnabled(scenario)) Debug.Log(obj.Invoke(), context);
		}
		public static async void LogAsync(TracingScenario scenario, Func<Task<object>> obj, Object context = null)
		{
			if (IsEnabled(scenario)) Debug.Log(await obj.Invoke(), context);
		}

#if HAS_HIDE_IN_CALLSTACKS
		[HideInCallstack]
#endif
		public static void LogWarning(TracingScenario scenario, object obj, Object context = null)
		{
			if (IsEnabled(scenario)) Debug.LogWarning(obj, context);
		}

#if HAS_HIDE_IN_CALLSTACKS
		[HideInCallstack]
#endif
		public static void LogError(TracingScenario scenario, object obj, Object context = null)
		{
			if (IsEnabled(scenario)) Debug.LogError(obj, context);
		}

#if HAS_HIDE_IN_CALLSTACKS
		[HideInCallstack]
#endif
		public static void LogException(TracingScenario scenario, Exception e, Object context = null)
		{
			if (IsEnabled(scenario)) Debug.LogException(e, context);
		}

		public static bool IsEnabled(TracingScenario scenario)
		{
			if (scenario == TracingScenario.Any) return true;
			return currentTracingScenarios.HasFlag(scenario);
		}

		// We cache it to not access EditorPrefs every time we need it
		private static int cachedTracingScenario = -1;

		private static TracingScenario currentTracingScenarios
		{
			get
			{
				if (cachedTracingScenario != -1)
					return (TracingScenario)cachedTracingScenario;
#if UNITY_EDITOR
				if(UnityThreads.IsMainThread())
					cachedTracingScenario = EditorPrefs.GetInt("NeedleTracingScenario", 0);
				else cachedTracingScenario = 0;
#else
				cachedTracingScenario = 0;
#endif
				return (TracingScenario)cachedTracingScenario;
			}
			set
			{
				if (cachedTracingScenario == (int)value) return;
				cachedTracingScenario = (int)value;
#if UNITY_EDITOR
				EditorPrefs.SetInt("NeedleTracingScenario", (int)value);
#endif
			}
		}

#if UNITY_EDITOR
		private class TracingScenarioEditor : EditorWindow
		{
			[MenuItem("Needle Engine/Internal/Tracing Scenarios", false, -1000)]
			private static void Open()
			{
				var window = GetWindow<TracingScenarioEditor>();
				if (window == null) window = CreateInstance<TracingScenarioEditor>();
				window.Show();
			}

			private void OnEnable()
			{
				this.titleContent = new GUIContent("Tracing Scenarios");
			}

			private readonly string[] tracingScenarioTypes = Enum.GetNames(typeof(TracingScenario));
			private readonly Array tracingScenarioValues = Enum.GetValues(typeof(TracingScenario));

			private void OnGUI()
			{
				var newValue = 0;
				for (var i = 0; i < tracingScenarioTypes.Length; i++)
				{
					var val = (TracingScenario)tracingScenarioValues.GetValue(i);
					if ((int)val == 0) continue;
					var isFlagEnabled =
						currentTracingScenarios.HasFlag((TracingScenario)tracingScenarioValues.GetValue(i));
					var enabled = EditorGUILayout.ToggleLeft(ObjectNames.NicifyVariableName(tracingScenarioValues.GetValue(i).ToString()),
						isFlagEnabled);
					newValue |= enabled ? (int)tracingScenarioValues.GetValue(i) : 0;
				}
				currentTracingScenarios = (TracingScenario)newValue;
			}
		}
#endif
	}
}