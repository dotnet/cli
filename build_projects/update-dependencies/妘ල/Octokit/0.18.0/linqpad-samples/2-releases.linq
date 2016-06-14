<Query Kind="Program">
  <NuGetReference>Octokit</NuGetReference>
  <NuGetReference>Octokit.Reactive</NuGetReference>
  <NuGetReference>Rx-Main</NuGetReference>
  <Namespace>Octokit</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main(string[] args)
{
	var owner = string.Empty;
	var reponame = string.Empty;
	
	owner = "octokit";
	reponame = "octokit.net";
	
	var client = new GitHubClient(new Octokit.ProductHeaderValue("octokit.samples"));
	
	//Get releases for the octokit
	var releases = await client.Release.GetAll(owner, reponame);
	releases.Select(r => new { r.Name, r.Body }).Dump("Releases");
	
	//Don't want draft release and because we are using reactive the first one is the latest one.
	var latestrelease = releases.First(r => r.Draft == false).Dump("Latest Octokit"); 
	
	//Gets the assets for the latest relase
	var assets = await client.Release.GetAllAssets(owner,reponame,latestrelease.Id);
	assets.Dump("Assets");
	var latestreleaseassetid = assets.First(a => a.Name.Contains("Reactive")).Id;
	var asset = await client.Release.GetAsset(owner,reponame,latestreleaseassetid);
	asset.DownloadCount.Dump("Download Count for this release");
	
	//Download the release
	var wc = new WebClient();
	var filename = Path.Combine( Path.GetTempPath(),"Octokit-Reactive.nupkg"); 
	await wc.DownloadFileTaskAsync(asset.BrowserDownloadUrl,filename);
	filename.Dump();
}

// Define other methods and classes here