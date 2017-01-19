## Getting help on migration issues
You've decided to try the new .NET Core tools that are MSBuild-based. You took your project.json project and ran `dotnet migrate` or migrated from Visual Studio 2017...and you maybe ran into problems. 

The best way to get help is to [file an issue](https://github.com/dotnet/cli/issues/new) on this repo and we will investigate and provide help and/or fixes as part of new CLI builds. 

### Filing an migration issue 
CLI is a very high-traffic repository in terms of issues. In order to be able to respond fast to migration issues, we need the issue to be formatted in a certain way:

* Add `[MIGRATION]:` prefix to the title of the issue.
* Add your `project.json` to the issue either attaching it or copy-pasting the contents into the issue.
    * If you migrated, the file can be found in the `backup` folder in your project folder (it is moved there by migration automatically).
* Add all of the errors that any operation like `dotnet restore`, `dotnet build` or others reported. This will help us speedily triage where the potential problem will be. 
* Mention @blackdwarf and @livarcc in the issue body. 

From there on, we will start investigating the issue and respond. 
