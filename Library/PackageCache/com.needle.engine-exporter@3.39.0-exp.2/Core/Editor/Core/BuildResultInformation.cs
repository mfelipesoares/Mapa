#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Needle.Engine.Editors;
using Needle.Engine.Problems;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Needle.Engine.Core
{
	[Serializable]
	public class BuildResultInformation
	{
		[InitializeOnLoadMethod]
		public static void Init()
		{
			Builder.BuildStarting += Clear;
			Builder.BuildEnding += () =>
			{
				var hasProblems = infos.Any(p => p.Severity == ProblemSeverity.Error);
				groupedByDescriptions.Clear();
				var byGroup = infos.GroupBy(p => p.ActionDescription);
				foreach (var group in byGroup)
				{
					var groupName = group.Key;
					if (groupName == null) continue;
					var list = group.ToList();
					groupedByDescriptions.Add(groupName, list);
				}
				if (!hasProblems) return;
				BuildProblemWindow.Open();
				throw new InvalidBuildException(); 
			};
		}

		private static void Clear()
		{
			infos.Clear();
			groupedByDescriptions.Clear();
		}
		
		public static IReadOnlyList<BuildResultInformation> Infos => infos;
		public static Dictionary<string, List<BuildResultInformation>> GroupedByDescriptions => groupedByDescriptions;
		public static bool HasAnyProblems => infos.Any(p => p.Severity == ProblemSeverity.Error);

		private static readonly List<BuildResultInformation> infos = new List<BuildResultInformation>();
		private static readonly Dictionary<string, List<BuildResultInformation>> groupedByDescriptions = new Dictionary<string, List<BuildResultInformation>>();

		
		public static void ReportBuildError(string message, Object context, LicenseType requiredLicense)
		{
			var bi = new BuildResultInformation(message, context, ProblemSeverity.Error);
			bi.RequiresLicense = requiredLicense;
			infos.Add(bi);
		}
		
		public static void ReportBuildProblem(string message, Object context, LicenseType requiredLicense)
		{
			var bi = new BuildResultInformation(message, context, ProblemSeverity.Warning);
			bi.RequiresLicense = requiredLicense;
			infos.Add(bi);
		}

		public static void Report(BuildResultInformation info)
		{
			infos.Add(info);
		}

		public string Message;
		public Object Context;
		public ProblemSeverity Severity;
		public LicenseType RequiresLicense;
		public string? ActionDescription;
		// public Action? Action;

		public BuildResultInformation(string message, Object context, ProblemSeverity severity)
		{
			Message = message;
			Severity = severity;
			Context = context;
		}
	}
}