using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AssemblyBrowserLib
{
    public class AssemblyTreeNode
    {
        public AssemblyTreeNode(string title)
        {
            Title = title;
        }

        public string Title { get; set; }
        public List<AssemblyTreeNode> ChildNodes { get; set; } = new();
    }
}