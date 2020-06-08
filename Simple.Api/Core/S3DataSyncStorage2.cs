using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Newtonsoft.Json;
using Simple.Api.Contract;

namespace Simple.Api.Core
{
    public class S3DataSyncStorage2<TDataItem>
    {
        private string _bucketName { get; set; }

        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.APNortheast1;

        private static IAmazonS3 _client;

        public S3DataSyncStorage2(string bucketName)
        {
            _bucketName = bucketName;
            _client = new AmazonS3Client(bucketRegion);
        }

        public bool Create(
            string syncId,
            IEnumerable<TDataItem> items,
            int pageSize,
            IDictionary<string, object> metadata)
        {
            var util = new TransferUtility(_client);

            int count = 0;
            int page = 0;
            var readOffset = 0;
            const int CHUNCK_SIZE = 1024 * 1024;//1MB
            string rootFolder = $"{Path.GetTempPath()}/{syncId}";

            Directory.CreateDirectory(rootFolder);

            try
            {
                using (var buffer = new MemoryStream())
                {
                    foreach (var item in items)
                    {
                        count++;

                        if (item is string)
                        {
                            var bytes = Encoding.UTF8.GetBytes(item as string);
                            buffer.Write(bytes, 0, bytes.Length);
                            readOffset += bytes.Length;
                        }
                        else
                        {
                            var bytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(item));
                            buffer.Write(bytes, 0, bytes.Length);
                            readOffset += bytes.Length;
                        }

                        //加上換行符號
                        buffer.WriteByte(10);
                        readOffset++;

                        if (count % pageSize == 0)
                        {
                            using (var m = new MemoryStream(buffer.GetBuffer(), 0, readOffset))
                            using (var f = new FileStream($"{rootFolder}/{page}.json", FileMode.Append))
                            {
                                //將剩於的資料寫入檔案,在以檔案上傳至S3
                                m.CopyTo(f);
                            }
                            using (var f = new FileStream($"{rootFolder}/{page}.json", FileMode.Open))
                            {
                                util.Upload(f, _bucketName, $"{syncId}/{page}.json");
                            }
                            page = count / pageSize;
                            readOffset = 0;
                            buffer.Position = 0;
                        }

                        //buffer如果滿了,先FLUSH
                        if (readOffset > CHUNCK_SIZE)
                        {
                            using (var m = new MemoryStream(buffer.GetBuffer(), 0, readOffset))
                            using (var f = new FileStream($"{rootFolder}/{page}.json", FileMode.Append))
                            {
                                m.CopyTo(f);
                            }
                            readOffset = 0;
                            buffer.Position = 0;
                        }
                    }

                    if (readOffset > 0)
                    {
                        page = count / pageSize;
                        using (var m = new MemoryStream(buffer.GetBuffer(), 0, readOffset))
                        using (var f = new FileStream($"{rootFolder}/{page}.json", FileMode.Append))
                        {
                            m.CopyTo(f);
                        }
                        using (var f = new FileStream($"{rootFolder}/{page}.json", FileMode.Open))
                        {
                            util.Upload(f, _bucketName, $"{syncId}/{page}.json");
                        }
                        readOffset = 0;
                        buffer.Position = 0;
                    }
                }

                var info = new InfoDto()
                {
                    PageSize = pageSize,
                    TotalPage = (int)Math.Ceiling((double)count / pageSize),
                    TotalItem = count,
                    Metadata = metadata
                };

                var infoBytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(info));
                using (var ms = new MemoryStream(infoBytes))
                {
                    util.Upload(ms, _bucketName, $"{syncId}/index.json");
                }
            }
            finally
            {
                Directory.Delete(rootFolder, true);
            }

            return true;
        }

        public bool Delete(string syncId)
        {
            try
            {
                var deleteObjectsRequest = new DeleteObjectsRequest
                {
                    BucketName = _bucketName
                };

                var listObjectRequest = new ListObjectsRequest
                {
                    BucketName = _bucketName,
                    Prefix = syncId
                };

                var listObjectResponse = _client.ListObjectsAsync(listObjectRequest).Result;

                foreach (S3Object entry in listObjectResponse.S3Objects)
                {
                    deleteObjectsRequest.AddKey(entry.Key);
                }

                var deleteObjectResponse = _client.DeleteObjectsAsync(deleteObjectsRequest).Result;

                return true;
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when deleting an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when deleting an object", e.Message);
            }
            return false;
        }

        public IEnumerable<TDataItem> GetDataPage(string syncId, int page)
        {
            var json = JsonSerializer.Create();
            var stream = this.ReadObjectData(syncId, $"{page}.json", 0, long.MaxValue);
            using (StreamReader reader = new StreamReader(stream))
            {
                var jsonReader = new JsonTextReader(reader);
                jsonReader.SupportMultipleContent = true;

                while (jsonReader.Read() == true)
                {
                    yield return json.Deserialize<TDataItem>(jsonReader);
                }

                yield break;
            }
        }

        public InfoDto GetInfo(string syncId)
        {
            var output = "";
            var stream = this.ReadObjectData(syncId, "index.json", 0, long.MaxValue);
            using (StreamReader reader = new StreamReader(stream))
            {
                output = reader.ReadToEnd();
            }

            var info = System.Text.Json.JsonSerializer.Deserialize<InfoDto>(output);
            return info;
        }

        public Stream GetStreamPage(string sessionId, int page,
                                    long from, long to)
        {
            return this.ReadObjectData(sessionId, $"{page}.json", from, to);
        }

        private void UploadObject(string content, string keyName)
        {
            var util = new TransferUtility(_client);
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            using (var ms = new MemoryStream(bytes))
            {
                try
                {
                    util.Upload(ms, _bucketName, keyName);
                }
                catch (AmazonS3Exception e)
                {
                    Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                }

            }
        }

        private Stream ReadObjectData(string syncId, string keyName, long from, long to)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = $"{syncId}/{keyName}",
                    ByteRange = new ByteRange(from, to)
                };
                var response = _client.GetObjectAsync(request).Result;
                //Console.WriteLine("[       ]"+response.ResponseStream.Length);
                Console.WriteLine("[       ]" + response.AcceptRanges);
                Console.WriteLine("[       ]" + response.ContentLength);
                Console.WriteLine("[       ]" + response.ContentRange);
                Console.WriteLine("[       ]" + response.ResponseStream.CanSeek);
                // var memory=new MemoryStream();
                //     response.ResponseStream.CopyTo(memory);
                //     return memory;


                return response.ResponseStream;
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered ***. Message:'{0}' when reading an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when reading an object", e.Message);
            }
            return null;
        }


        public string GetPreSignedUrl(string objectKey, DateTime? expire)
        {
            var preSignedUrlExpire = expire ?? DateTime.Now.AddMinutes(5);

            var request = new GetPreSignedUrlRequest()
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = preSignedUrlExpire
            };

            return _client.GetPreSignedURL(request);
        }
    }
}