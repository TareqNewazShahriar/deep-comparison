# How to publish NuGet packages

### Publish command
- Change to the folder containing the .nupkg file (/project/bin/Release (or Debug)/_.nupkg).
- Run the following command, specifying your package name (unique package ID) and replacing the key value with your API key:
```
dotnet nuget push PackageName.1.0.0.nupkg --api-key __key__ --source https://api.nuget.org/v3/index.json
```
