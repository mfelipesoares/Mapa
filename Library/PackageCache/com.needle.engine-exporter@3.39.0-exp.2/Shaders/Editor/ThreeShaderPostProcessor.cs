using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Needle.Engine.Shaders
{
	public class ThreeShaderPostProcessor
	{
		public void PostProcessShaders(ExtensionData data)
		{
			if (data == null) return;
			foreach (var sh in data.shaders)
				PostProcessShader(sh);
		}

		// https://regex101.com/r/ji65Gq/1
		private static readonly Regex blockStartRegex = new Regex(@".*?uniform\s+.+?\s+\{", RegexOptions.Compiled);

		private static readonly Regex replaceLayoutRegex = new Regex(@"layout\(.+?\)\s+?");

		// https://regex101.com/r/5966Ku/1
		private static readonly Regex removeUpperCaseDefinesWithArg = new Regex(@"[A-Z_]+\(\d\)\s+?");

		public void PostProcessShader(ExtensionData.Shader shader)
		{
			if (shader == null) throw new ArgumentNullException(nameof(shader));

			var code = shader.code;
			var didHaveCode = !string.IsNullOrWhiteSpace(code);
			if (string.IsNullOrEmpty(code))
			{
				if (string.IsNullOrEmpty(shader.uri))
				{
					code = ExtensionData.Shader.GetCode(shader.uri);
				}
			}

			if (string.IsNullOrWhiteSpace(code)) throw new Exception("No shader code provided for " + shader.name);
			var reader = new StringReader(code);
			var res = new StringBuilder();
			var foundAnyText = false;

			string line;
			var isInUnsupportedUniformBlock = false;
			var isInMain = false;
			var skipNextLine = false;
			do
			{
				line = reader.ReadLine();
				if (skipNextLine) {
					skipNextLine = false;
					continue;
				}
				
				if (line != null)
				{
					if (line.Contains("#version"))
					{
						line = line.Replace("310", "300");
						line += "\n// downgraded version from 310 (unsupported shader version)";
					}
					if (!foundAnyText && string.IsNullOrWhiteSpace(line)) continue;
					
					var isInvalidLine = false;
					if (line.StartsWith("#define") || line.StartsWith("#if") || line.StartsWith("#endif") || line.StartsWith("#else"))
					{
						if(!line.StartsWith("#define SV_TARGET0 gl_FragData[0]")) // WebGL1-specific
						{
							line = "// " + line;
							isInvalidLine = true;
						}
					}
					if (line.Contains("precision mediump float;")) // WebGL1-specific
					{
						line = "// " + line;
						isInvalidLine = true;
					}
					if (line.Contains("uniform") && blockStartRegex.IsMatch(line))
					{
						isInUnsupportedUniformBlock = true;
						line = "\n// removed unsupported block: " + line;
						isInvalidLine = true;
					}
					else if (isInUnsupportedUniformBlock)
					{
						if (line.StartsWith("}"))
						{
							isInUnsupportedUniformBlock = false;
							continue;
						}

						if (!isInvalidLine)
						{
							line = line.Replace("UNITY_UNIFORM", "");
							line = "uniform " + line.TrimStart();
						}
					}

					line = replaceLayoutRegex.Replace(line, "");
					line = removeUpperCaseDefinesWithArg.Replace(line, "");
					
					if (line.Contains("GL_EXT_shader_texture_lod") || line.Contains("GL_EXT_shader_framebuffer_fetch") || line.TrimStart().StartsWith("inout "))
					{
						line = "// " + line;
						isInvalidLine = true;
					}

					// if (line.StartsWith("uniform")) line = line.Replace("uniform", "uniform highp");

					// if (line.StartsWith("attribute highp vec"))
					// 	line = "//" + line;
					line = line.Replace("in_NORMAL0", "normal");
					line = line.Replace("in_TANGENT0", "tangent");
					line = line.Replace("in_POSITION0", "position");
					line = line.Replace("in_TEXCOORD0", "uv");
					line = line.Replace("in_TEXCOORD1", "uv2");
					line = line.Replace("in_TEXCOORD2", "uv3");
					line = line.Replace("in_TEXCOORD3", "uv4");
					line = line.Replace("in_COLOR0", "color");

					if (shader.type == ExtensionData.ShaderType.Vertex)
					{
						// insert any other defines right before the position is declared.
						if (line.Contains("in highp vec3 position;"))
						{
							res.AppendLine("#ifdef USE_INSTANCING");
							res.AppendLine("  in mat4 instanceMatrix;");
							res.AppendLine("#endif");
								
							res.AppendLine(line);
							foundAnyText = true;
							continue;
						}
						
						// right after main(), we want to add any code that modifies input variables.
						// we also need to make sure that we introduce intermediary variables that we can modify;
						// the in variables are read-only.
						if (line.Contains("void main()"))
						{
							isInMain = true;
							foundAnyText = true;
							res.AppendLine(line + " {");
							skipNextLine = true;
							
							// We're moving the object2world matrix into a variable so that we can optionally apply instancing etc.
							res.AppendLine("vec4 osw[4] = hlslcc_mtx4x4unity_ObjectToWorld;");
							
							// apply instancing matrix multiply to osw[4] with instanceMatrix - could also apply skinning matrix here probably
							res.AppendLine("#ifdef USE_INSTANCING");
							res.AppendLine("mat4 _osw;");
							res.AppendLine("_osw[0] = hlslcc_mtx4x4unity_ObjectToWorld[0];");
							res.AppendLine("_osw[1] = hlslcc_mtx4x4unity_ObjectToWorld[1];");
							res.AppendLine("_osw[2] = hlslcc_mtx4x4unity_ObjectToWorld[2];");
							res.AppendLine("_osw[3] = hlslcc_mtx4x4unity_ObjectToWorld[3];");
							res.AppendLine("_osw = instanceMatrix * _osw;");
							res.AppendLine("osw[0] = _osw[0];");
							res.AppendLine("osw[1] = _osw[1];");
							res.AppendLine("osw[2] = _osw[2];");
							res.AppendLine("osw[3] = _osw[3];");
							res.AppendLine("#endif");
							
							continue;
						}

						if (isInMain)
						{
							// use the new variable in further code paths
							line = line.Replace("hlslcc_mtx4x4unity_ObjectToWorld", "osw");
						}
					}
					foundAnyText = true;
					res.AppendLine(line);
				}
			} while (line != null);

			var newCode = res.ToString();
			if (didHaveCode)
				shader.code = newCode;
			shader.uri = ExtensionData.Shader.GetUri(newCode);
		}
	}
}