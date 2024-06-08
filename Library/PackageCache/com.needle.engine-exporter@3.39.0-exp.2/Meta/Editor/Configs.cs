using Needle.Engine.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Needle.Engine
{
	internal class MetaConfigProperty : IBuildConfigProperty
	{
		public string Key => "meta";
		public object GetValue(string projectDirectory) => Object.FindAnyObjectByType<HtmlMeta>()?.meta;
	}

    internal class AbsoluteUrl : IBuildConfigProperty
    {
        public string Key => "absolutePath";
        public object GetValue(string projectDirectory)
        {
	        var context = Builder.CurrentContext;
	        if (context is ExportContext exp && exp.BuildContext != null)
	        {
		        if(!string.IsNullOrEmpty(exp.BuildContext.LiveUrl))
			        return exp.BuildContext.LiveUrl;
		        if(!exp.BuildContext.IsDistributionBuild)
			        return "https://localhost:3000";
	        }
	        return null;
        }
    }
    
    internal class ProjectName : IBuildConfigProperty
    {
	    public string Key => "sceneName";
	    public object GetValue(string projectDirectory)
	    {
		    return UnityEditor.ObjectNames.NicifyVariableName(SceneManager.GetActiveScene().name);
	    }
    }

    internal class DeployOnly : IBuildConfigProperty
    {
	    public string Key => "deployOnly";
	    
	    public object GetValue(string projectDirectory)
	    {
		    var context = Builder.CurrentContext;
		    if (context is ExportContext ctx)
		    {
			    return ctx.BuildContext.Command == BuildCommand.PrepareDeploy;
		    }
		    
		    return false;
	    }
    }
}