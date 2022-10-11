namespace storage
{
    public class CloudStorageOptions
    {
        public enum StorageType
        {
            AzureBlob,
            S3,
            Redis
        }

        public StorageType Type { get; set; }
        public AzureBlobOptions? AzureBlob { get; set; }
        public S3Options? S3Options { get; set; }
    }
}
