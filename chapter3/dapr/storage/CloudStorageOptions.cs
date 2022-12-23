namespace storage;

public class CloudStorageOptions
{
    public enum StorageType
    {
        AzureBlob,
        AwsS3,
        Local
    }

    public StorageType Type { get; set; }
}
