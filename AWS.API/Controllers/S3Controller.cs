using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AWSStorage.API.Model;
using System.Net;

namespace AWS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class S3StorageController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3StorageController> _logger;
        private readonly string _bucketName = "";

        public S3StorageController(ILogger<S3StorageController> logger, IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
            _logger = logger;
            _bucketName = Environment.GetEnvironmentVariable("AWSS3Bucket");
        }

        [HttpGet]
        [Route("index")]
        public IActionResult Index()
        {
            return Ok("AWSS3 API is Running....");
        }



        [HttpPost]
        [Route("uploadfile")]
        public async Task<S3ResponseModel> Uploadfile(S3RequestModel request)
        {
            try
            {
                string key = request.key;
                if (request.fileContent == null || request.fileContent["$content"] == null)
                {
                    return new S3ResponseModel { Success = false, Message = "No file content was provided." };
                }

                byte[] fileContent = Convert.FromBase64String(request.fileContent["$content"]);
                using (var memoryStream = new MemoryStream(fileContent))
                {
                    var fileTransferUtility = new TransferUtility(_s3Client);

                    await fileTransferUtility.UploadAsync(memoryStream, _bucketName, key);
                }

                _logger.LogInformation($"File {request.key} uploaded successfully");
                return new S3ResponseModel { Success = true, Message = $"File {request.key} uploaded successfully" };
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                _logger.LogError(ex, errorMessage);
                return new S3ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
            }
        }



        [HttpGet]
        [Route("downloadfile")]
        public async Task<IActionResult> DownloadFile(string key)
        {

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };
                var response = await _s3Client.GetObjectAsync(request);
                var fileStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(fileStream);
                fileStream.Seek(0, SeekOrigin.Begin);
                _logger.LogInformation($"File {key} downloaded successfully");
                //return File(fileStream, response.Headers.ContentType, fileName);
                return new FileStreamResult(fileStream, response.Headers.ContentType)
                {
                    FileDownloadName = key
                };

            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "The requested file was not found.");
                    return NotFound(new S3ResponseModel { Success = false, Message = "The requested file was not found.", ErrorCode = 404 });
                }
                else
                {
                    _logger.LogError(ex, $"An error occurred: {ex.Message}");
                    return StatusCode(500, new S3ResponseModel { Success = false, Message = $"An error occurred: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred: {ex.Message}");
                return StatusCode(500, new S3ResponseModel { Success = false, Message = $"An error occurred: {ex.Message}" });
            }

        }


        [HttpDelete]
        [Route("deletefile")]
        public async Task<S3ResponseModel> DeleteFile(string fileName)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName
                };
                var response = await _s3Client.DeleteObjectAsync(request);

                _logger.LogInformation($"File {fileName} deleted successfully");
                return new S3ResponseModel { Success = true, Message = $"File {fileName} deleted successfully" };
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "The requested file was not found.");
                    return new S3ResponseModel { Success = false, Message = "The requested file was not found.", ErrorCode = 404 };
                }
                else
                {
                    var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                    _logger.LogError(ex, errorMessage);
                    return new S3ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };

                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                _logger.LogError(ex, errorMessage);
                return new S3ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
            }
        }

        [HttpGet]
        [Route("listfile")]
        public async Task<S3ResponseModel> Listfile()
        {
            try
            {
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _bucketName
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
                _logger.LogInformation("List of file(s) successfully retrieved");
                return new S3ResponseModel { Success = true, Data = listResponse.S3Objects };
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "The requested file was not found.");
                    return new S3ResponseModel { Success = false, Message = "The requested file was not found.", ErrorCode = 404 };
                }
                else
                {
                    var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                    _logger.LogError(ex, errorMessage);
                    return new S3ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                _logger.LogError(ex, errorMessage);
                return new S3ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
            }
        }


    }
}
