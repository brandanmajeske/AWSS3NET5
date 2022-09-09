using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;

namespace AWS.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AWSController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _client;

        public AWSController(IConfiguration config)
        {
            _config = config;
            var awsAccess = _config.GetValue<string>("AWSSDK:AccessKey");
            var awsSecret = _config.GetValue<string>("AWSSDK:SecretKey");

            _client = new AmazonS3Client(awsAccess, awsSecret, Amazon.RegionEndpoint.USWest2);
        }

        [HttpGet("ListBuckets")]
        public async Task<IActionResult> ListBuckets()
        {
            try
            {
                var result = await _client.ListBucketsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Buckets could not be listed");
            }
        }

        [HttpGet("ListObjects/{bucketName}")]
        public async Task<IActionResult> ListObjects(string bucketName)
        {
            try
            {
               ListObjectsRequest objectsRequest = new ListObjectsRequest()
                {
                    BucketName = bucketName,
                };
                ListObjectsResponse response = await _client.ListObjectsAsync(objectsRequest);
                return Ok(response);
            }
            catch (Exception)
            {
                return BadRequest($"Objects could not be listed");
            }
        }

        [HttpGet("GenerateDownloadLink/{bucketName}/{keyName}")]
        public IActionResult GenerateDownloadLink(string bucketName, string keyName)
        {
            try
            {
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    Expires = DateTime.Now.AddHours(5),
                    Protocol = Protocol.HTTP
                };
                string downloadLink = _client.GetPreSignedURL(request);
                return Ok(downloadLink);
            }
            catch (Exception)
            {
                return BadRequest("Download link was not generated");
            }
        }


        [HttpPost("CreateBucket/{name}")]
        public async Task<IActionResult> CreateBucket(string name)
        {
            try
            {
                PutBucketRequest request = new PutBucketRequest() { BucketName = name };
                await _client.PutBucketAsync(request);
                return Ok($"Bucket: {name} WAS created");
            }
            catch (Exception ex)
            {
                return BadRequest($"Bucket: {name} WAS NOT created");
            }
        }

        [HttpPost("CreateObject/{bucketName}/{objectName}")]
        public async Task<IActionResult> CreateObject(string bucketName, string objectName)
        {
            try
            {
                FileInfo file = new FileInfo(@"C:\AWSFiles\thankyou.txt");

                PutObjectRequest request = new PutObjectRequest()
                {
                     InputStream = file.OpenRead(),
                     BucketName = bucketName, 
                     Key = "Thankyou.txt",
                 };
                await _client.PutObjectAsync(request);

                ListObjectsRequest objectsRequest = new ListObjectsRequest()
                {
                    BucketName = bucketName,
                };
                ListObjectsResponse response = await _client.ListObjectsAsync(objectsRequest);

                //return Ok($"Object: {objectName} WAS created/uploaded");
                return Ok(response);
             }
            catch (Exception)
            {
                return BadRequest($"Object: {objectName} WAS NOT created/uploaded");
            }
        }

        [HttpPost("CreateFolder/{bucketName}/{folderName}")]
        public async Task<IActionResult> CreateFolder(string bucketName, string folderName)
        {
            try
            {
                PutObjectRequest request = new PutObjectRequest() 
                { 
                    BucketName = bucketName, 
                    Key = folderName.Replace("%2F", "/") 
                };

                await _client.PutObjectAsync(request);
                return Ok($"{folderName} WAS created");
            }
            catch (Exception ex)
            {
                return BadRequest("Folder could not be created");
            }
        }

        [HttpPost("EnableVersioning/{bucketName}")]
        public async Task<IActionResult> EnableVersioning(string bucketName)
        {
            try
            {
                PutBucketVersioningRequest request = new PutBucketVersioningRequest()
                {
                    BucketName = bucketName,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Enabled
                    }
                };

                await _client.PutBucketVersioningAsync(request);
                return Ok($"Bucket {bucketName} Versioning ENABLED");
            }
            catch (Exception ex)
            {
                return BadRequest($"Bucket {bucketName} Versioning NOT ENABLED");
            }
        }

        [HttpPut("AddMetadata/{bucketName}/{fileName}")]
        public async Task<IActionResult> AddMetadata(string bucketName, string fileName)
        {

            try
            {
            Tagging newTags = new Tagging()
            {
                TagSet = new List<Tag>
                {
                    new Tag {Key = "Key1", Value = "FirstTag"},
                    new Tag {Key = "Key2", Value = "SecondTag"},
                }
            };

            PutObjectTaggingRequest request = new PutObjectTaggingRequest()
            {
                BucketName = bucketName,
                Key = fileName,
                Tagging = newTags
            };

            await _client.PutObjectTaggingAsync(request);
            return Ok("Tags added!");

            }
            catch (Exception ex)
            {
                return BadRequest("Tags NOT added!");
            }

        }


        [HttpPut("CopyFile/{sourceBucket}/{sourceKey}/{destinationBucket}/{destinationKey}")]
        public async Task<IActionResult> CopyFile(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey)
        {

            try
            {
                CopyObjectRequest request = new CopyObjectRequest()
                {
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey,
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey
                };

                
                await _client.CopyObjectAsync(request);
                return Ok("Object/File copied!");

            }
            catch (Exception ex)
            {
                return BadRequest("Object/File NOT copied!");
            }

        }

        [HttpDelete("DeleteBucket/{bucketName}")]
        public async Task<IActionResult> DeleteBucket(string bucketName)
        {
            try
            {
               DeleteBucketRequest request = new DeleteBucketRequest() { BucketName = bucketName};

                await _client.DeleteBucketAsync(request);
                return Ok($"{bucketName} WAS deleted");
            }
            catch (Exception ex)
            {
                return BadRequest($"{bucketName} WAS NOT deleted");
            }
        }

        [HttpDelete("DeleteBucketObject/{bucketName}/{objectName}")]
        public async Task<IActionResult> DeleteBucketObject(string bucketName, string objectName)
        {
            try
            {
                DeleteObjectRequest request = new DeleteObjectRequest() { BucketName = bucketName, Key = objectName };

                await _client.DeleteObjectAsync(request);
                return Ok($"{objectName} in {bucketName} WAS deleted");
            }
            catch (Exception ex)
            {
                return BadRequest($"{objectName} in {bucketName} WAS NOT deleted");
            }
        }

        [HttpDelete("CleanUpBucket/{bucketName}")]
        public async Task<IActionResult> CleanUpBucket(string bucketName)
        {
            try
            {
                DeleteObjectsRequest request = new DeleteObjectsRequest() 
                { 
                    BucketName = bucketName,
                    Objects = new List<KeyVersion>
                    {
                        new KeyVersion() {Key = "Thankyou.txt"},
                        new KeyVersion() {Key = "welcome.txt"}
                    }
                };

                await _client.DeleteObjectsAsync(request);
                return Ok($"{bucketName} WAS emptied");
            }
            catch (Exception ex)
            {
                return BadRequest($"{bucketName} WAS NOT emptied");
            }
        }
    }
}
