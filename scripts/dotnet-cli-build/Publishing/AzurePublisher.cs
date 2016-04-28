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

namespace Microsoft.DotNet.Cli.Build
{
    public class AzurePublisher
    {
        private static readonly string s_dotnetBlobRootUrl = "https://dotnetcli.blob.core.windows.net/dotnet/";
        private static readonly string s_dotnetBlobContainerName = "dotnet";

        private string _connectionString { get; set; }
        private CloudBlobContainer _blobContainer { get; set; }

        public AzurePublisher()
        {
            _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING").Trim('"');
            _blobContainer = GetDotnetBlobContainer(_connectionString);
        }

        private CloudBlobContainer GetDotnetBlobContainer(string connectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            
            return blobClient.GetContainerReference(s_dotnetBlobContainerName);
        }

        public void PublishInstallerFile(string installerFile, string channel, string version)
        {
            var installerFileBlob = CalculateInstallerBlob(installerFile, channel, version);
            PublishFile(installerFileBlob, installerFile);
        }

        public void PublishArchive(string archiveFile, string channel, string version)
        {
            var archiveFileBlob = CalculateArchiveBlob(archiveFile, channel, version);
            PublishFile(archiveFileBlob, archiveFile);
        }

        public void PublishFile(string blob, string file)
        {
            CloudBlockBlob blockBlob = _blobContainer.GetBlockBlobReference(blob);
            blockBlob.UploadFromFileAsync(file, FileMode.Open).Wait();

            SetBlobPropertiesBasedOnFileType(blockBlob);
        }

        public void PublishStringToBlob(string blob, string content)
        {
            CloudBlockBlob blockBlob = _blobContainer.GetBlockBlobReference(blob);
            blockBlob.UploadTextAsync(content).Wait();
        }

        public void CopyBlob(string sourceBlob, string targetBlob)
        {
            CloudBlockBlob source = _blobContainer.GetBlockBlobReference(sourceBlob);
            CloudBlockBlob target = _blobContainer.GetBlockBlobReference(targetBlob);

            // Create the empty blob
            using (MemoryStream ms = new MemoryStream())
            {
                target.UploadFromStreamAsync(ms).Wait();
            }

            // Copy actual blob data
            target.StartCopyAsync(source).Wait();
        }

        private void SetBlobPropertiesBasedOnFileType(CloudBlockBlob blockBlob)
        {
            if (Path.GetExtension(blockBlob.Uri.AbsolutePath.ToLower()) == ".svg")
            {
                blockBlob.Properties.ContentType = "image/svg+xml";
                blockBlob.Properties.CacheControl = "no-cache";
                blockBlob.SetPropertiesAsync().Wait();
            }
        }

        public IEnumerable<string> ListBlobs(string virtualDirectory)
        {
            CloudBlobDirectory blobDir = _blobContainer.GetDirectoryReference(virtualDirectory);
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

        public void DropLatestSpecifiedVersion(string version)
        {
            CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(version);
            using (MemoryStream ms = new MemoryStream())
            {
                blob.UploadFromStreamAsync(ms).Wait();
            }
        }

        public void DeleteBlob(string path)
        {
            _blobContainer.GetBlockBlobReference(path).DeleteAsync().Wait();
        }

        public string CalculateInstallerUploadUrl(string installerFile, string channel, string version)
        {
            return $"{s_dotnetBlobRootUrl}{CalculateInstallerBlob(installerFile, channel, version)}";
        }

        public string CalculateInstallerBlob(string installerFile, string channel, string version)
        {
            return $"{channel}/Installers/{version}/{Path.GetFileName(installerFile)}";
        }

        public string CalculateArchiveUploadUrl(string archiveFile, string channel, string version)
        {
            return $"{s_dotnetBlobRootUrl}{CalculateArchiveBlob(archiveFile, channel, version)}";
        }

        public string CalculateArchiveBlob(string archiveFile, string channel, string version)
        {
            return $"{channel}/Binaries/{version}/{Path.GetFileName(archiveFile)}";
        }
    }
}
