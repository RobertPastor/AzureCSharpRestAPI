using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageRestApiAuth
{
    public class BlobStorageData
    {
        public string storageAccountName { get; set; }
        public DateTime now { get; set; }
        public List<BlobContainerData> containers ;
        public BlobStorageData(string _storageAccountName)
        {
            storageAccountName = _storageAccountName;
            now =  DateTime.UtcNow;
            containers = new List<BlobContainerData>();
        }
    }

    public class BlobContainerData
    {
        public string containerName { get; set; }
        public DateTime now { get; set; }
        public List<Blob> blobs;

        public BlobContainerData(string _containerName)
        {
            containerName = _containerName;
            now = DateTime.UtcNow;
            blobs = new List<Blob>();
        }
    }

    public class Blob
    {
        public string blobName { get; set; }
        public string lastModified;
        public string contentLengthBytes;
        public Blob(string _blobName, string _lastModified, string _contentLength)
        {
            blobName = _blobName;
            lastModified = _lastModified;
            contentLengthBytes = _contentLength;
        }
    }
}
