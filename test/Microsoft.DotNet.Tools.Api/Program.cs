using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Api
{
    public class Program
    {
        public static string HttpGet(string requestUri)
        {
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(requestUri);
                WebResponse response = request.GetResponseAsync().Result;
                if (request.HaveResponse)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch { /* choke all errors */ }

            return null;
        }

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet api <string>");
                Console.WriteLine("Example: dotnet api xmlReader");
                Console.WriteLine("Example: dotnet api xmlRe*r");
                return 0;
            }

            UrlEncoder urlEncoder = UrlEncoder.Create(UnicodeRanges.BasicLatin);
            string searchTerm = urlEncoder.Encode(string.Join(" ", args));
            string requestUri = $"http://packagesearch.azurewebsites.net/Search/?searchTerm={searchTerm}";

            string response = HttpGet(requestUri);
            if (response == null)
            {
                Console.WriteLine("Could not connect with the server.");
                return 1;
            }

            var results = JsonConvert.DeserializeObject<List<SearchResult>>(response);
            bool hasResults = false;
            foreach (var result in results)
            {
                if (result.PackageDetails == null)
                {
                    continue;
                }

                hasResults = true;

                Console.Write($"Type: {result.FullTypeName}");
                if (result.Signature != null)
                {
                    Console.Write($": {result.Signature}");
                }

                Console.WriteLine($" in {result.PackageDetails.Name} {result.PackageDetails.Version}");
            }

            if (!hasResults)
            {
                Console.WriteLine("No results found.");
                Console.WriteLine("Tip: If you don't remember the full name try using * in the query");
                Console.WriteLine($"Try also using it in the browser: http://packagesearch.azurewebsites.net/?q=*{searchTerm}*");
            }
            else
            {
                Console.WriteLine($"See results also in the browser: http://packagesearch.azurewebsites.net/?q={searchTerm}");
            }

            return 0;
        }
    }
}
