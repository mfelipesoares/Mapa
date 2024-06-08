using System.Threading.Tasks;
using Needle.Engine.Problems;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Editors
{
	public class ProjectValidationWindow : EditorWindow
	{
		[MenuItem(Constants.MenuItemRoot + "/Project Validation ✅", false, Constants.MenuItemOrder- 997)]
		public static void Open()
		{
			var existing = GetWindow<ProjectValidationWindow>();
			if (existing)
			{
				existing.Show();
				Refresh();
			}
			else
			{
				existing = CreateInstance<ProjectValidationWindow>();
				existing.Show();				
				Refresh();
			}

			async void Refresh()
			{
				await Task.Delay(500);
				ProjectValidation.Instance.Refresh();
			}
		}

		private void OnEnable()
		{
			minSize = new Vector2(550, 640);
			maxSize = new Vector2(550, 800);
			titleContent = new GUIContent("Project Validation", Assets.Logo);
		}

		private void OnGUI()
		{
			if (docked)
			{
				// undock
				position = position;
			}
			ProjectValidation.DrawProjectValidationUI();
		}
	}


}