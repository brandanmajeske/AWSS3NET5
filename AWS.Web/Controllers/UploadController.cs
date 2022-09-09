﻿using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _client;

        public UploadController(IConfiguration config)
        {
            _config = config;
            var awsAccess = _config.GetValue<string>("AWSSDK:AccessKey");
            var awsSecret = _config.GetValue<string>("AWSSDK:SecretKey");

            _client = new AmazonS3Client(awsAccess, awsSecret, RegionEndpoint.USWest2);
        }

        [HttpPost("UploadFile/{bucketName}")]
        public async Task<IActionResult> UploadFile(string bucketName)
        {
            var fileStream = CreateDataStream();
            var fileKey = "multipart_upload_file.mp4";

            try
            {
                // initiate multipart upload
                InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest()
                {
                    BucketName = bucketName,
                    Key = fileKey,
                };

                InitiateMultipartUploadResponse initiateResponse = await _client.InitiateMultipartUploadAsync(initiateRequest);

                var contentLength = fileStream.Length;
                int chunkSize = 5 * 1024 * 1024;//5*(int)Math.Pow(2, 10); // chunks of 5MB
                int chunkSizeOLD = 5*(int)Math.Pow(2, 10); // old
                var chunksList = new List<PartETag>(); // multipart upload parts chunks

                try
                {
                    int filePosition = 0; // start at beginning of file
                    for( int i = 1; filePosition < contentLength; i++)
                    {
                        UploadPartRequest uploadPartRequest = new UploadPartRequest()
                        {
                            BucketName = bucketName,
                            Key = fileKey,
                            UploadId = initiateResponse.UploadId,
                            PartNumber = i,
                            PartSize = chunkSize,
                            InputStream = fileStream
                        };

                        uploadPartRequest.StreamTransferProgress += new EventHandler<StreamTransferProgressArgs>(UploadPartProgressEventCallback);

                        UploadPartResponse uploadPartResponse = await _client.UploadPartAsync(uploadPartRequest);
                        chunksList.Add(new PartETag()
                        {
                            ETag = uploadPartResponse.ETag,
                            PartNumber = i
                        });

                        filePosition += chunkSize;
                    }

                    // Complete Upload

                    CompleteMultipartUploadRequest completeMultipartUploadRequest = new CompleteMultipartUploadRequest()
                    {
                        BucketName = bucketName,
                        Key = fileKey,
                        UploadId = initiateResponse.UploadId,
                        PartETags = chunksList,
                    };
                    await _client.CompleteMultipartUploadAsync(completeMultipartUploadRequest);
                }
                catch (Exception)
                {

                    AbortMultipartUploadRequest abortRequest = new AbortMultipartUploadRequest()
                    {
                        BucketName = bucketName,
                        Key = fileKey,
                        UploadId = initiateResponse.UploadId
                    };

                    await _client.AbortMultipartUploadAsync(abortRequest);
                    return BadRequest("File COULD NOT be uploaded");
                }

            }
            catch (Exception ex)
            {
                throw;
            }

            return Ok();
        }

        private void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"{e.TransferredBytes}/{e.TotalBytes}");
        }

        private FileStream CreateDataStream()
        {
            FileStream fileStream = System.IO.File.OpenRead("Uploads/kazookid.mp4");
            return fileStream;
        }
    }
}
