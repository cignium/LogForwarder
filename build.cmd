nuget restore

"%programfiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe" WatcherLog\WatcherLog.csproj /p:OutDir="..\artifacts";Configuration=Release;SolutionDir="."

nuget pack