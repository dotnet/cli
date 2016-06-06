using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class AzurePublisher
    {
        public enum Product
        {
            SharedFramework,
            Host,
            SDK,
            HostAndFramework,
            FrameworkAndSdk,
            HostAndFrameworkAndSdk,
        }

        private static readonly string s_dotnetBlobRootUrl = "https://dotnetcli.blob.core.windows.net/dotnet/";
        private static readonly string s_dotnetBlobContainerName = "dotnet";

        private string _connectionString { get; set; }
        private CloudBlobContainer _blobContainer { get; set; }

        public AzurePublisher()
        {
            _connectionString = EnvVars.EnsureVariable("CONNECTION_STRING").Trim('"');
            _blobContainer = GetDotnetBlobContainer(_connectionString);
        }

        private CloudBlobContainer GetDotnetBlobContainer(string connectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            
            return blobClient.GetContainerReference(s_dotnetBlobContainerName);
        }

        public string UploadFile(string file, Product product, string version)
        {
            string url = CalculateUploadUrlForFile(file, product, version);
            CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(url);
            blob.UploadFromFileAsync(file, FileMode.Open).Wait();
            SetBlobPropertiesBasedOnFileType(blob);
            return url;
        }

        public static async Task DownloadFile(string name, Product product, string version, string localPath)
        {
            string url = CalculateUploadUrlForFile(name, product, version);
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var sendTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                var response = sendTask.Result.EnsureSuccessStatusCode();
                
                var httpStream = await response.Content.ReadAsStreamAsync();
                using (var fs = File.Create(localPath))
                using (var reader = new StreamReader(httpStream))
                {
                    httpStream.CopyTo(fs);
                    fs.Flush();
                }
            }
        }

        public void DownloadFiles(Product product, string version, string fileExtension, string downloadPath)
        {
            string url = CalculateUploadDirectory(product, version);
            CloudBlobDirectory blobDir = _blobContainer.GetDirectoryReference(url);
            BlobContinuationToken continuationToken = new BlobContinuationToken();

            var blobFiles = blobDir.ListBlobsSegmentedAsync(continuationToken).Result;

            foreach (var blobFile in blobFiles.Results.OfType<CloudBlockBlob>())
            {
                if (Path.GetExtension(blobFile.Uri.AbsoluteUri) == fileExtension)
                {
                    string localBlobFile = Path.Combine(downloadPath, Path.GetFileName(blobFile.Uri.AbsoluteUri));
                    Console.WriteLine($"Downloading {blobFile.Uri.AbsoluteUri} to {localBlobFile}...");
                    blobFile.DownloadToFileAsync(localBlobFile, FileMode.Create).Wait();
                }
            }
        }

        private void SetBlobPropertiesBasedOnFileType(CloudBlockBlob blockBlob)
        {
            if (Path.GetExtension(blockBlob.Uri.AbsolutePath.ToLower()) == ".svg")
            {
                blockBlob.Properties.ContentType = "image/svg+xml";
                blockBlob.Properties.CacheControl = "no-cache";
                blockBlob.SetPropertiesAsync().Wait();
            }
            else if (Path.GetExtension(blockBlob.Uri.AbsolutePath.ToLower()) == ".version")
            {
                blockBlob.Properties.ContentType = "text/plain";
                blockBlob.SetPropertiesAsync().Wait();
            }
        }

        public IEnumerable<string> ListBlobs(Product product, string version)
        {
            string url = CalculateUploadDirectory(product, version);
            CloudBlobDirectory blobDir = _blobContainer.GetDirectoryReference(url);
            BlobContinuationToken continuationToken = new BlobContinuationToken();

            var blobFiles = blobDir.ListBlobsSegmentedAsync(continuationToken).Result;
            return blobFiles.Results.Select(bf => bf.Uri.PathAndQuery);
        }

        public string AcquireLeaseOnBlob(string blob)
        {
            CloudBlockBlob cloudBlob = _blobContainer.GetBlockBlobReference(blob);
            System.Threading.Tasks.Task<string> task = cloudBlob.AcquireLeaseAsync(TimeSpan.FromMinutes(1), null);
            task.Wait(); 
            return task.Result;
        }

        public void ReleaseLeaseOnBlob(string blob, string leaseId)
        {
            CloudBlockBlob cloudBlob = _blobContainer.GetBlockBlobReference(blob);
            AccessCondition ac = new AccessCondition() { LeaseId = leaseId };
            cloudBlob.ReleaseLeaseAsync(ac).Wait();
        }

        public bool IsLatestSpecifiedVersion(string version)
        {
            System.Threading.Tasks.Task<bool> task = _blobContainer.GetBlockBlobReference(version).ExistsAsync();
            task.Wait();
            return task.Result;
        }

        public bool TryDeleteBlob(string path)
        {
            try
            {
                DeleteBlob(path);
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Deleting blob {path} failed with \r\n{e.Message}");
                
                return false;
            }
        }

        private void DeleteBlob(string path)
        {
            _blobContainer.GetBlockBlobReference(path).DeleteAsync().Wait();
        }

        private static string CalculateUploadUrlForFile(string file, Product product, string version)
        {
            return $"{CalculateUploadDirectory(product, version)}/{Path.GetFileName(file)}";
        }

        private static string CalculateUploadDirectory(Product product, string version)
        {
            return $"{s_dotnetBlobRootUrl}packages/{product}/{version}";
        }
    }
}
