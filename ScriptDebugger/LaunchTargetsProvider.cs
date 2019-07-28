using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Clide;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace ScriptDebugger
{
    [Export(typeof(IDebugProfileLaunchTargetsProvider))]
    [AppliesTo(Constants.AppliesTo)]
    [Order(50)]
    class LaunchTargetsProvider : IDebugProfileLaunchTargetsProvider
    {
        readonly ConfiguredProject configuredProject;
        readonly Lazy<IProjectNode> projectNode;

        [ImportingConstructor]
        public LaunchTargetsProvider(
            ConfiguredProject configuredProject,
            [Import(Constants.ProjectNodeContractName)] Lazy<IProjectNode> projectNode)
        {
            this.configuredProject = configuredProject;
            this.projectNode = projectNode;
        }

        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public bool SupportsProfile(ILaunchProfile profile) => true;

        public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            var properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
            var targetPath = await properties.GetEvaluatedPropertyValueAsync("TargetPath");
            var installDir = await properties.GetEvaluatedPropertyValueAsync("VsInstallRoot");
            var scriptFile = projectNode.Value.Nodes.OfType<IItemNode>().FirstOrDefault(i => i.IsSelected);

            if (scriptFile == null)
                return new List<IDebugLaunchSettings>();

            var csx = scriptFile.PhysicalPath.EndsWith(".csx", StringComparison.OrdinalIgnoreCase);
            var fsx = scriptFile.PhysicalPath.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase);

            // Can't get fsx to attach the debugger, so don't support it for now.
            if (!csx)
                return new List<IDebugLaunchSettings>();

            var exe = csx ?
                Path.Combine(installDir, "MSBuild\\Current\\Bin\\Roslyn\\csi.exe") :
                Path.Combine(installDir, "Common7\\IDE\\CommonExtensions\\Microsoft\\FSharp\\fsi.exe");

            if (!File.Exists(exe))
                return new List<IDebugLaunchSettings>();

            var launchSettings = new DebugLaunchSettings(launchOptions)
            {
                Executable = exe,
                Arguments = " \"" + scriptFile.PhysicalPath + "\"",
                Project = projectNode.Value.AsVsHierarchy(),
                CurrentDirectory = Path.GetDirectoryName(scriptFile.PhysicalPath),
                LaunchOperation = DebugLaunchOperation.CreateProcess,
                LaunchDebugEngineGuid = DebuggerEngines.ManagedOnlyEngine,
                SendToOutputWindow = true,
            };

            return new List<IDebugLaunchSettings>() { launchSettings };
        }
    }
}