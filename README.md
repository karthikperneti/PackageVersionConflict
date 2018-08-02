# Package Version Conflict Finder
To find the various versions of same package is used in a Same Solution or In a Source Code.

Over the time, In most of the Projects, We end up having different versions of same package reference in various assemblies. Which will create issues. 
By default, When we install a package from nuget, It will suggest the latest version. We will end up having various versions. 
To monitor this problem we can do two things.
1. Create a unit test which will read all the package.config files and *.csproj files and finds the conflict of versions.
2. Create a utility which will read all the package.config files and *.csproj files and creates a report of packages versions used by the projects.

