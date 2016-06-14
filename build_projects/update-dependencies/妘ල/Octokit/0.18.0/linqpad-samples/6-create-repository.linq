<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.dll</Reference>
  <NuGetReference>Octokit</NuGetReference>
  <NuGetReference>Octokit.Reactive</NuGetReference>
  <NuGetReference>Rx-Main</NuGetReference>
  <Namespace>Octokit</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main(string[] args)
{
	var owner = string.Empty;
	var reponame = string.Empty;

	GitHubClient client = new GitHubClient(
		new Octokit.ProductHeaderValue("Octokit.samples"));

	owner = "naveensrinivasan";
	reponame = "my-awesome-repo-" + Environment.TickCount;
	// or if you don't want to give an app your creds
	// you can use a token from an OAuth app
	// Here is the URL to get tokens https://github.com/settings/tokens
	// and save the token using Util.SetPassword("github","CHANGETHIS")
	client.Credentials = new Credentials(Util.GetPassword("github"));
		
	
	var email = "person@cooldomain.com";
	
	// 1 - create a repository through the API
	var newRepo = new NewRepository(reponame)
	{
		AutoInit = true // very helpful!
	};

	var repository = await client.Repository.Create(newRepo);
	Console.WriteLine("Browse the repository at: " + repository.HtmlUrl);
	
	// 2 - create a blob containing the contents of our README
	var newBlob = new NewBlob() { 
	   Content = "#MY AWESOME REPO\rthis is some code\rI made it on: "
	   	+ DateTime.Now.ToString(), 
	   Encoding = EncodingType.Utf8 
	};
	
	var createdBlob = await client.GitDatabase.Blob
		.Create(owner, reponame, newBlob);
	createdBlob.Dump();
	
	// 3 - create a tree which represents just the README file
	var newTree = new NewTree();
	newTree.Tree.Add(new NewTreeItem() { 
	  Path = "README.md", 
	  Mode = Octokit.FileMode.File,
	  Sha = createdBlob.Sha,
	  Type = TreeType.Blob
	});
	
	var createdTree = await client.GitDatabase.Tree
		.Create(owner, reponame, newTree);
	
	createdTree.Dump();
	
	// 4 - this commit should build on the current master branch
	var master = await client.GitDatabase.Reference
		.Get(owner, reponame, "heads/master");
	
	var newCommit = new NewCommit(
	  "Hello World!",
	  createdTree.Sha,
	  new[] { master.Object.Sha })
	{ Author = new Committer(owner,email,DateTime.UtcNow)};
	
	var createdCommit = await client.GitDatabase.Commit
		.Create(owner, reponame, newCommit);
	
	createdCommit.Dump();
	
	// 5 - create a reference for the master branch
	var updateReference = new ReferenceUpdate(createdCommit.Sha);
	var updatedReference = await client.GitDatabase
		.Reference.Update(owner, reponame, "heads/master", updateReference);
	updatedReference.Dump();
	
	// Deleting a repository requires admin access. 
	//If OAuth is used, the `delete_repo` scope is required.
	await client.Repository.Delete(owner, reponame);
	"Repo Clean up!".Dump(reponame + " has been deleted");
}