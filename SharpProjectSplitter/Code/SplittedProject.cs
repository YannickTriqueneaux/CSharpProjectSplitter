using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpProjectSplitter
{
    public class SplittedProject
    {
        public Dictionary<string, FileDependencies[]> Folders { get; }
        public List<string> ExplicitlySplittedFolders { get; }
        public List<Project> SplittedProjects { get; private set; }

        public SplittedProject(Dictionary<string, FileDependencies[]> folders, List<string> explicitlySplittedFolders)
        {
            Folders = folders;
            ExplicitlySplittedFolders = explicitlySplittedFolders;
        }

        internal void AnalyzeAndSplit(int deptAccepted = 2)
        {
            foreach (var folder in Folders)
                foreach (var file in folder.Value)
                    file.Pass1(Folders);
            foreach (var folder in Folders)
                foreach (var file in folder.Value)
                    file.Pass2(Folders);


            var projects = new Dictionary<string, Project>();
            foreach (var folder in Folders)
                projects.Add(folder.Key, new Project(folder.Key, folder.Value));

            int iterationCount = 0;
            var projectsToJoin = projects;
            var joinedProjects = new Dictionary<string, Project>(projects.Count);
            bool anyJoined;
            do
            {
                anyJoined = false;
                ++iterationCount;
                foreach (var project in projectsToJoin)
                    project.Value.Pass1(projectsToJoin);
                foreach (var project in projectsToJoin)
                    project.Value.Pass2();


                foreach (var p in projectsToJoin)
                {
                    var currentDeptAccepted = deptAccepted + ExplicitlySplittedFolders.Count(f => p.Key.StartsWith(f));

                    if (p.Value.FolderName.Split('\\').Length > currentDeptAccepted)
                    {
                        string parentName = string.Join("\\", p.Value.FolderName.Split('\\').Take(currentDeptAccepted));

                        if (projectsToJoin.TryGetValue(parentName, out Project parent))
                            parent.MergeChild(p.Value);
                        else if (joinedProjects.TryGetValue(parentName, out Project parent2))
                            parent2.MergeChild(p.Value);
                        else
                        {
                            var newParent = new Project(parentName, p.Value.Files);
                            newParent.MergeChild(p.Value);
                            joinedProjects.Add(parentName, newParent);
                        }
                        anyJoined = true;
                    }
                    else
                    {
                        joinedProjects.Add(p.Key, p.Value);
                    }
                }
                projectsToJoin = joinedProjects;
                joinedProjects = new Dictionary<string, Project>();
            }
            while (anyJoined);


            foreach (var project in projectsToJoin)
                project.Value.Pass1(projectsToJoin);
            foreach (var project in projectsToJoin)
                project.Value.Pass2();

            joinedProjects = projectsToJoin;


            SplittedProjects = projectsToJoin.Values.ToList();
        }

    }
}