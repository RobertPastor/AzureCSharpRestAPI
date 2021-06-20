namespace StorageRestApiAuth
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.IO;
    
    using Newtonsoft.Json;

    internal static class Program
    {
        static string StorageAccountName = "amsairdatastorageaccount";
        static string StorageAccountKey = "";
        static FileStream fs;
        static BlobStorageData blobStorageData;

        private static void Main()
        {

            // use ReadLine() to read the entered line
            Console.WriteLine("In the Storage Account, use left hand side menu to get one Access Key");
            Console.WriteLine("In the Storage Account, click overview to ensure that Storage account key access is Enabled");
            Console.WriteLine("Please enter the Storage Account Access Key");
            StorageAccountKey = Console.ReadLine();

            // display the line
            Console.WriteLine("Storage Account Access Key = {0}", StorageAccountKey);

            // local path
            string sPath = System.AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine(sPath);
            string outputFileName = "AMSAirDataBlobStorage-" + DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss") + "-GMT" + ".json";
            sPath = System.IO.Path.Combine(sPath, outputFileName);
            try
            {
                blobStorageData = new BlobStorageData(StorageAccountName);
                fs = File.Create(sPath);
                // List the containers in a storage account.
                ListContainersAsyncREST(StorageAccountName, StorageAccountKey, CancellationToken.None).GetAwaiter().GetResult();
                string json = JsonConvert.SerializeObject(blobStorageData, Formatting.Indented);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("It is finished -> Press any key to end the program.");
            Console.ReadLine();
        }

        /// <summary>
        /// This is the method to call the REST API to retrieve a list of
        /// blobs in the specific storage account container
        private static async Task ListContainerContentAsyncREST(BlobContainerData blobContainerData, string storageAccountName, string containerName, string storageAccountKey, CancellationToken cancellationToken)
        {

            // Construct the URI. This will look like this:
            //   https://myaccount.blob.core.windows.net/resource
            //   https://docs.microsoft.com/en-us/rest/api/storageservices/list-blobs
            String uri = string.Format("https://{0}.blob.core.windows.net/{1}/?restype=container&comp=list", storageAccountName, containerName);

            Console.WriteLine(uri);

            // Set this to whatever payload you desire. Ours is null because 
            //   we're not passing anything in.
            Byte[] requestPayload = null;

            //specify to use TLS 1.2 as default connection
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Instantiate the request message with a null payload.
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
            { Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload) })
            {

                // Add the request headers for x-ms-date and x-ms-version.
                DateTime now = DateTime.UtcNow;
                httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
                // check for a given version what are the response fields
                httpRequestMessage.Headers.Add("x-ms-version", "2009-09-19");
                // If you need any additional headers, add them here before creating
                //   the authorization header. 

                // Add the authorization header.
                httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetAuthorizationHeader(
                   storageAccountName, storageAccountKey, now, httpRequestMessage);
                
                // Send the request.
                using (HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, cancellationToken))
                {
                    // If successful (status code = 200), 
                    //   parse the XML response for the container names.
                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        Console.WriteLine(httpResponseMessage.ToString());

                        String xmlString = await httpResponseMessage.Content.ReadAsStringAsync();
                        XElement x = XElement.Parse(xmlString);
                        foreach (XElement blob in x.Element("Blobs").Elements("Blob"))
                        {
                            Console.WriteLine("Blob name = {0}", blob.Element("Name").Value);
                            Console.WriteLine("Blob Last Modified = {0}", blob.Element("Properties").Element("Last-Modified").Value);
                            Console.WriteLine("Blob Content-Length = {0}", blob.Element("Properties").Element("Content-Length").Value);
                            Console.WriteLine("-----------------");
                            Blob blobContent = new Blob(blob.Element("Name").Value, blob.Element("Properties").Element("Last-Modified").Value, blob.Element("Properties").Element("Content-Length").Value);
                            blobContainerData.blobs.Add(blobContent);

                        }
                    }
                    else
                    {
                        Console.WriteLine(httpResponseMessage.ToString());
                        Console.WriteLine("Status code = {0}", httpResponseMessage.StatusCode.ToString());
                    }
                }
            }
        }

            /// <summary>
            /// This is the method to call the REST API to retrieve a list of
            /// containers in the specific storage account.
            /// This will call CreateRESTRequest to create the request, 
            /// then check the returned status code. If it's OK (200), it will 
            /// parse the response and show the list of containers found.
            /// </summary>
            private static async Task ListContainersAsyncREST(string storageAccountName, string storageAccountKey, CancellationToken cancellationToken)
        {

            // Construct the URI. This will look like this:
            //   https://myaccount.blob.core.windows.net/resource
            String uri = string.Format("https://{0}.blob.core.windows.net/?comp=list", storageAccountName);

            Console.WriteLine(uri);

            // Set this to whatever payload you desire. Ours is null because 
            //   we're not passing anything in.
            Byte[] requestPayload = null;

            //specify to use TLS 1.2 as default connection
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Instantiate the request message with a null payload.
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
            { Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload) })
            {
                // Add the request headers for x-ms-date and x-ms-version.
                DateTime now = DateTime.UtcNow;
                httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
                httpRequestMessage.Headers.Add("x-ms-version", "2017-04-17");
                // If you need any additional headers, add them here before creating
                //   the authorization header. 

                // Add the authorization header.
                httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetAuthorizationHeader(
                   storageAccountName, storageAccountKey, now, httpRequestMessage);

                if (httpRequestMessage.Headers.Authorization == null)
                {
                    Console.WriteLine("====> problem raised while building the Authorization header");
                }
                else
                {
                    // Send the request.
                    using (HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, cancellationToken))
                    {
                        // If successful (status code = 200), 
                        //   parse the XML response for the container names.
                        if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                        {
                            Console.WriteLine(httpResponseMessage.ToString());

                            String xmlString = await httpResponseMessage.Content.ReadAsStringAsync();
                            XElement x = XElement.Parse(xmlString);
                            foreach (XElement container in x.Element("Containers").Elements("Container"))
                            {
                                Console.WriteLine("Container name = {0}", container.Element("Name").Value);

                                BlobContainerData blobContainerData = new BlobContainerData(container.Element("Name").Value);
                                // List blobs in a container
                                string containerName = container.Element("Name").Value;
                                ListContainerContentAsyncREST(blobContainerData, StorageAccountName, containerName, StorageAccountKey, CancellationToken.None).GetAwaiter().GetResult();

                                blobStorageData.containers.Add(blobContainerData);
                            }
                        }
                        else
                        {
                            Console.WriteLine(httpResponseMessage.ToString());
                            Console.WriteLine("Status code = {0}", httpResponseMessage.StatusCode.ToString());
                        }
                    }
                }
            }
        }
    }
}
