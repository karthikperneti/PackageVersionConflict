using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace KP.PackageVersionConflictLibray
{
    public class Processor
    {

        //PATH CONSTANTS
        private const string SOURCE_DIRECTORY_NAME = "src/";
        private const string OUTPUT_FILE_NAME = "Projects-Packages-{0}.csv";

        //PACKAGE CONFIG CONSTANTS - Net Framework
        private const string PACKAGES_CONFIG_FILE_NAME = "packages.config";
        private const string PACKAGES_NODE_NAME = "packages";
        private const string PACKAGE_ID_ATTRIBUTE_NAME = "id";
        private const string PACKAGE_VERSION_ATTRIBUTE_NAME = "version";

        //PROJECT CONFIG CONSTANTS - .Net Core Framework
        private const string ITEMGROUP_NODE_NAME = "ItemGroup";
        private const string ITEMGROUP_PACKAGE_ID_ATTRIBUTE_NAME = "Include";
        private const string ITEMGROUP_PACKAGE_VERSION_ATTRIBUTE_NAME = "Version";


        public IDictionary<string, ICollection<string>> packageVersionsById = new Dictionary<string, ICollection<string>>();
        public IDictionary<string, string[]> projectInfoCollection = new Dictionary<string, string[]>();

        public List<string> projectNames = new List<string>();
        public List<string> packageFiles = new List<string>();
        public List<string> projectFiles = new List<string>();
        public List<string> allFiles = new List<string>();

        /// <summary>
        /// Get the package.config files in the source directory.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetAllPackagesConfigFilePaths()
        {
            return Directory.GetFiles(GetSourceDirectoryPath(), PACKAGES_CONFIG_FILE_NAME, SearchOption.AllDirectories);
        }
        /// <summary>
        /// Get all the CSPROJ Files in the source directory after removing the unit test projects.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetAllProjectConfigFilePaths()
        {
            return Directory.GetFiles(GetSourceDirectoryPath(), "*.csproj", SearchOption.AllDirectories).Where(x => !x.Contains("unittest"));
        }
        /// <summary>
        /// Get the source path using current library path.
        /// </summary>
        /// <returns></returns>
        private string GetSourceDirectoryPath()
        {
            // string codeBase = @"C:\KP\Source\TerraMicroService";
            string codeBase = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string baseDirectory = Path.GetDirectoryName(path.Substring(0, path.IndexOf(SOURCE_DIRECTORY_NAME, StringComparison.Ordinal) + SOURCE_DIRECTORY_NAME.Length));
            if (string.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = path.Substring(0, path.IndexOf(SOURCE_DIRECTORY_NAME, StringComparison.Ordinal) + SOURCE_DIRECTORY_NAME.Length);
            }
            return baseDirectory;
            //return path;
        }

        public void PackageInfoUpdate(string packageId, string packageVersion, string projectName)
        {
            if (!projectNames.Exists(x => x == projectName)) projectNames.Add(projectName);
            string[] line = new string[allFiles.Count];
            if (!packageVersionsById.TryGetValue(packageId, out ICollection<string> packageVersions))
            {
                packageVersions = new List<string>();
                packageVersionsById.Add(packageId, packageVersions);
            }

            if (!packageVersions.Contains(packageVersion))
            {
                packageVersions.Add(packageVersion);
            }

            //Project Info Collection
            if (!projectInfoCollection.TryGetValue(packageId, out string[] projectInfoVersions))
            {
                projectInfoVersions = new string[allFiles.Count];
                projectInfoCollection.Add(packageId, projectInfoVersions);
            }
            var index = projectNames.IndexOf(projectName);
            projectInfoVersions[index] = packageVersion;
        }

        public void Process()
        {
            packageFiles = GetAllPackagesConfigFilePaths().ToList();
            projectFiles = GetAllProjectConfigFilePaths().ToList();
            allFiles.AddRange(packageFiles);
            allFiles.AddRange(projectFiles);

            foreach (string packagesConfigFilePath in packageFiles)
            {
                var doc = new XmlDocument();
                doc.Load(packagesConfigFilePath);

                XmlNode packagesNode = doc.SelectSingleNode(PACKAGES_NODE_NAME);
                if (packagesNode != null && packagesNode.HasChildNodes)
                {
                    foreach (var packageNode in packagesNode.ChildNodes.Cast<XmlNode>())
                    {
                        if (packageNode.Attributes == null)
                        {
                            continue;
                        }

                        string packageId = packageNode.Attributes[PACKAGE_ID_ATTRIBUTE_NAME].Value;
                        string packageVersion = packageNode.Attributes[PACKAGE_VERSION_ATTRIBUTE_NAME].Value;
                        string projectName = Path.GetFileName(Path.GetDirectoryName(packagesConfigFilePath));
                        PackageInfoUpdate(packageId, packageVersion, projectName);
                    }
                }
            }

            foreach (string packagesConfigFilePath in projectFiles)
            {
                var doc = new XmlDocument();
                doc.Load(packagesConfigFilePath);

                // XmlNode packagesNode = doc.SelectSingleNode(ITEMGROUP_NODE_NAME);
                XmlNodeList xmlNodeList = doc.SelectNodes("//ItemGroup//PackageReference");
                if (xmlNodeList != null)
                {
                    foreach (XmlNode packageNode in xmlNodeList)
                    {
                        if (packageNode.Attributes == null)
                        {
                            continue;
                        }

                        string packageId = packageNode.Attributes[ITEMGROUP_PACKAGE_ID_ATTRIBUTE_NAME].Value;
                        string packageVersion = packageNode.Attributes[ITEMGROUP_PACKAGE_VERSION_ATTRIBUTE_NAME].Value;
                        string projectName = Path.GetFileName(Path.GetDirectoryName(packagesConfigFilePath));

                        PackageInfoUpdate(packageId, packageVersion, projectName);

                    }
                }
            }




        }

        /// <summary>
        /// Generate CSV File
        /// </summary>
        public void PackageConflictReport()
        {
            List<string> data = new List<string>();
            projectNames.Insert(0, "");
            data.Add(string.Join(",", projectNames.ToArray()));
            foreach (var keyValue in projectInfoCollection)
            {
                string line = string.Format("{0}, {1}", keyValue.Key, string.Join(",", keyValue.Value));
                data.Add(line);
            }

            string outputFilePath = Path.Combine(GetSourceDirectoryPath(), string.Format(OUTPUT_FILE_NAME, DateTime.UtcNow.Ticks));
            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);

            File.WriteAllLines(outputFilePath, data);
        }

        /// <summary>
        /// To find the any conflicts is there or not.
        /// </summary>
        /// <returns></returns>
        public bool PackageConflictResult()
        {
            List<KeyValuePair<string, ICollection<string>>> packagesWithIncoherentVersions = packageVersionsById.Where(kv => kv.Value.Count > 1).ToList();


            string errorMessage = string.Empty;
            if (packagesWithIncoherentVersions.Any())
            {

                /* -- Print all the conflicts -- */
                /*
                errorMessage = $"Some referenced packages have incoherent versions. Please fix them by adapting the nuget reference:{Environment.NewLine}";
                foreach (var packagesWithIncoherentVersion in packagesWithIncoherentVersions)
                {
                    string packageName = packagesWithIncoherentVersion.Key;
                    string packageVersions = string.Join(", ", packagesWithIncoherentVersion.Value);
                    errorMessage += $"{packageName}: {packageVersions}{Environment.NewLine}";
                }
                */
                errorMessage = string.Format("{0} Conflicts in the Package Versions !!!", packagesWithIncoherentVersions.Count);
            }

            Console.WriteLine(errorMessage);

            return packagesWithIncoherentVersions.Any();
        }



    }



    public class ProjectInfo
    {
        public string ProjectName { get; set; }
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }

    }
}
