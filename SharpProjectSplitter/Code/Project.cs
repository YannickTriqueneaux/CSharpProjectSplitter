using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpProjectSplitter
{
    public class Project
    {
        public Project(string folderName, FileDependencies[] files)
        {
            FolderName = folderName;
            Files = files;
        }

        public string FolderName { get; }
        public FileDependencies[] Files { get; private set; }
        public Project[] Dependencies { get; private set; }
        public Project Parent { get; private set; }

        internal void Pass1(Dictionary<string, Project> projects)
        {
            string parentName = System.IO.Path.GetDirectoryName(FolderName + ".txt");
            if (projects.TryGetValue(parentName, out Project parent))
                Parent = parent;
            else
                Parent = null;

            foreach (var file in Files)
                file.AssignedProject = this;
        }
        internal void Pass2()
        {
            Dependencies = Files.SelectMany(f => f.Dependencies).Select(d => d.AssignedProject).Where(p => p != this).Distinct().ToArray();
        }

        internal void MergeChild(Project child)
        {
            Files = Files.Concat(child.Files).ToArray();
        }

        internal int GetParentDept()
        {
            int dept = 0;
            var parent = Parent;
            while(parent != null)
            {
                ++dept;
                parent = parent.Parent;
            }
            return dept;
        }

        public override string ToString()
        {
            return FolderName;
        }
    }
}