namespace AWSStorage.API.Model
{
    public class S3RequestModel
    {       
        public Dictionary<string, string> fileContent { get; set; }
        public string key { get; set; }

    }
}
