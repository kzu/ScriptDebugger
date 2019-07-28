using System;
using System.ComponentModel.Composition;
using Clide;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace ScriptDebugger
{
    /// <summary>
    /// Provides an <see cref="IProjectNode"/> for the current unconfigured project in CPS.
    /// </summary>
    class ProjectNodeProvider
    {
        readonly JoinableTaskContext jtc;
        readonly ISolutionExplorer solutionExplorer;
        readonly JoinableLazy<IProjectNode> projectNode;
        readonly UnconfiguredProject unconfiguredProject;

        [ImportingConstructor]
        public ProjectNodeProvider(
            JoinableTaskContext jtc,
            UnconfiguredProject unconfiguredProject,
            ISolutionExplorer solutionExplorer)
        {
            this.jtc = jtc;
            this.unconfiguredProject = unconfiguredProject;
            this.solutionExplorer = solutionExplorer;

            projectNode = new JoinableLazy<IProjectNode>(async () =>
                (await solutionExplorer.Solution)
                    .FindProject(x => string.Equals(
                            x.PhysicalPath,
                            unconfiguredProject.FullPath,
                            StringComparison.OrdinalIgnoreCase)), jtc.Factory);
        }

        [Export(Constants.ProjectNodeContractName, typeof(IProjectNode))]
        [AppliesTo(Constants.AppliesTo)]
        IProjectNode ProjectNode => projectNode.GetValue();
    }
}
