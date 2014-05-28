using System;
using System.IO;
using System.Linq;

namespace Ritter.AutomationDrivers.Core.Configuration
{
    public interface IProjectLocation
    {
        string FullPath { get; }
    }

    public class ProjectPath : IProjectLocation
    {
        public string FullPath { get; private set; }

        private ProjectPath(string fullPath)
        {
            var folder = new DirectoryInfo(fullPath);
            if (!folder.Exists)
            {
                throw new DirectoryNotFoundException();
            }
            FullPath = fullPath;
        }

        public static ProjectPath FromPath(string webProjectFullPath)
        {
            return new ProjectPath(webProjectFullPath);
        }

        public static ProjectPath FromFolder(string webProjectFolderName)
        {
            string solutionFolder = GetSolutionFolderPath();
            string projectPath = FindSubFolderPath(solutionFolder, webProjectFolderName);
            return new ProjectPath(projectPath);
        }

        private static string GetSolutionFolderPath()
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);

            while (directory != null && directory.GetFiles("*.sln").Length == 0)
            {
                directory = directory.Parent;
            }
            return directory.FullName;
        }

        private static string FindSubFolderPath(string rootFolderPath, string folderName)
        {
            var directory = new DirectoryInfo(rootFolderPath);

            directory = (directory.GetDirectories("*", SearchOption.AllDirectories)
                .Where(folder => folder.Name.ToLower() == folderName.ToLower()))
                .FirstOrDefault();

            if (directory == null)
            {
                throw new DirectoryNotFoundException();
            }

            return directory.FullName;
        }
    }
}
