namespace Grand.Infrastructure.Configuration
{
    public partial class AmazonConfig
    {
        /// <summary>
        /// Amazon Access Key
        /// </summary>
        public string AmazonAwsAccessKeyId { get; set; }

        /// <summary>
        /// Amazon Secret Access Key
        /// </summary>
        public string AmazonAwsSecretAccessKey { get; set; }

        /// <summary>
        /// Amazon Bucket Name using for identifying resources
        /// </summary>
        public string AmazonBucketName { get; set; }

        /// <summary>
        /// Amazon Domain name for cloudfront distribution
        /// </summary>
        public string AmazonDistributionDomainName { get; set; }

        /// <summary>
        /// Amazon Region 
        /// http://docs.amazonwebservices.com/AmazonS3/latest/BucketConfiguration.html#LocationSelection
        /// </summary>
        public string AmazonRegion { get; set; }
    }
}
