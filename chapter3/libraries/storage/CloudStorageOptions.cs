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
    public AzureBlobOptions? AzureBlob { get; set; }
    public AwsS3Options? AwsS3 { get; set; }
}
