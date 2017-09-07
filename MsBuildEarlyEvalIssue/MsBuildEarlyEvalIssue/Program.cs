using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildEarlyEvalIssue
{
    class Program
    {
        static void Main(string[] args)
        {
			InitializeMSBuild();

			var p = LoadProject(@"..\XTargetProject\XTargetProject.csproj", "");
			var items = p.GetItems("Compile");
		}

		private static Project LoadProject(string projectFile, string targetFramework)
		{
			var sw = Stopwatch.StartNew();
			BinaryLogger logger = null;

			try
			{
				var properties = new Dictionary<string, string>()
				{
					["Configuration"] = "Debug",
				};

				if (!string.IsNullOrEmpty(targetFramework))
				{
					properties["TargetFramework"] = targetFramework;
				}

				var xmlReader = XmlReader.Create(projectFile);
				var collection = new ProjectCollection();

				// Change this logger details to troubleshoot project loading details.
				collection.RegisterLogger(new Microsoft.Build.Logging.ConsoleLogger() { Verbosity = LoggerVerbosity.Minimal });
				collection.RegisterLogger(logger = new BinaryLogger() { Parameters = @"test.binlog;ProjectImports=ZipFile", Verbosity = LoggerVerbosity.Diagnostic });

				collection.OnlyLogCriticalEvents = false;
				var xml = Microsoft.Build.Construction.ProjectRootElement.Create(xmlReader, collection);

				// When constructing a project from an XmlReader, MSBuild cannot determine the project file path.  Setting the
				xml.FullPath = Path.GetFullPath(projectFile);

				return new Project(
					xml,
					properties,
					toolsVersion: null,
					projectCollection: collection
				);
			}
			finally
			{
				logger?.Shutdown();
				Console.WriteLine($"[{sw.Elapsed}] Loaded {projectFile} for {targetFramework}");
			}
		}

		private static void InitializeMSBuild()
		{
			var pi = new System.Diagnostics.ProcessStartInfo(
				"cmd.exe",
				@"/c ""C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"" -property installationPath"
			)
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			var process = System.Diagnostics.Process.Start(pi);
			process.WaitForExit();
			var installPath = process.StandardOutput.ReadLine();

			Environment.SetEnvironmentVariable("VSINSTALLDIR", installPath);
		}

	}
}
