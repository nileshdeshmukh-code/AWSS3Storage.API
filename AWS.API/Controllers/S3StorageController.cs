using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using Amazon.S3.Model;
using AWS.API.Model;
using Amazon.S3.Transfer;
using System.Net;

namespace AWS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class S3StorageController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3StorageController> _logger;
        private readonly string _bucketName;

        public S3StorageController(IConfiguration configuration, ILogger<S3StorageController> logger, IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
            _logger = logger;
            _bucketName = configuration.GetValue<string>("AWS:Bucket");
        }

        [HttpGet]
        [Route("index")]
        public IActionResult Index()
        {
            return Ok("AWSS3 API is Running....");
        }

        [HttpPost]
        [Route("upload")]
        public async Task<ResponseModel> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return new ResponseModel { Success = false, Message = "No file was uploaded." };
                }

                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileTransferUtility = new TransferUtility(_s3Client);
                    await fileTransferUtility.UploadAsync(memoryStream, _bucketName, file.FileName);
                }
                _logger.LogInformation($"File {file.FileName} uploaded successfully");
                return new ResponseModel { Success = true, Message = $"File {file.FileName} uploaded successfully"};
            }
            catch (Exception ex)
            {

                var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                _logger.LogError(ex, errorMessage);
                return new ResponseModel { Success = false, Message = errorMessage , ErrorCode=500};
            }
        }

        [HttpGet]
        [Route("download")]
        public async Task<IActionResult> Download(string fileName)
        {
                    
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName
                };
                var response = await _s3Client.GetObjectAsync(request);
                var fileStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(fileStream);
                fileStream.Seek(0, SeekOrigin.Begin);
                _logger.LogInformation($"File {fileName} downloaded successfully");                
                //return File(fileStream, response.Headers.ContentType, fileName);
                return new FileStreamResult(fileStream, response.Headers.ContentType)
                {
                    FileDownloadName = fileName
                };

            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "The requested file was not found.");
                    return NotFound(new ResponseModel { Success = false, Message = "The requested file was not found." ,ErrorCode=404});
                }
                else
                {
                    _logger.LogError(ex, $"An error occurred: {ex.Message}");
                    return StatusCode(500, new ResponseModel { Success = false, Message = $"An error occurred: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred: {ex.Message}");
                return StatusCode(500, new ResponseModel { Success = false, Message = $"An error occurred: {ex.Message}" });
            }
            
        }


        [HttpDelete]
        [Route("delete")]
        public async Task<ResponseModel> Delete(string fileName)
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
                return new ResponseModel { Success = true, Message = $"File {fileName} deleted successfully" };
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "The requested file was not found.");
                    return new ResponseModel { Success = false, Message = "The requested file was not found.", ErrorCode = 404 };
                }
                else
                {
                    var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                    _logger.LogError(ex, errorMessage);
                    return new ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
                    
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                _logger.LogError(ex, errorMessage);
                return new ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
            }
        }

        [HttpGet]
        [Route("list")]
        public async Task<ResponseModel> List()
        {
            try
            {
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _bucketName
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
                _logger.LogInformation("List of file(s) successfully retrieved");
                return new ResponseModel { Success = true, Data = listResponse.S3Objects };
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "The requested file was not found.");
                    return new ResponseModel { Success = false, Message = "The requested file was not found.", ErrorCode = 404 };
                }
                else
                {
                    var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                    _logger.LogError(ex, errorMessage);
                    return new ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred: {ex.GetType().Name}. {ex.Message}. Stack trace: {ex.StackTrace}";
                _logger.LogError(ex, errorMessage);
                return new ResponseModel { Success = false, Message = errorMessage, ErrorCode = 500 };
            }
        }

        
    }
}
