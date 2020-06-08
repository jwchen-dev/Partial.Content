using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Simple.Api.Core;

namespace Simple.Api.Controllers
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;

        private readonly S3DataSyncStorage<string> _storage;

        private readonly S3DataSyncStorage2<string> _storage2;

        public HomeController(ILogger<HomeController> logger,
                                S3DataSyncStorage<string> storage,
                                S3DataSyncStorage2<string> storage2)
        {
            _logger = logger;
            _storage = storage;
            _storage2 = storage2;
        }


        [HttpGet("~/Home/LocalFileStreamSimple")]
        public IActionResult LocalFileStreamSimple()
        {
            var file = File(System.IO.File.OpenRead("/bigjson.json"), "text/plain");
            return file;
        }


        [HttpGet("~/Home/LocalFileStream")]
        public IActionResult LocalFileStream()
        {
            var file = File(System.IO.File.OpenRead("/bigjson.json"), "text/plain", true);
            return file;
        }


        [HttpGet("~/Home/HttpClientStream")]
        public IActionResult HttpClientStream()
        {
            var client = new HttpClient();
            var response = client.GetAsync("http://www.google.com").Result;
            var file = File(response.Content.ReadAsStreamAsync().Result, "text/plain", true);
            return file;
        }

        [HttpGet("~/Home/PutToS3")]
        public IActionResult PutToS3(int dataCount, int pageSize)
        {
            var guid = Guid.NewGuid().ToString();
            _storage.Create(guid, GetData(dataCount), pageSize, new Dictionary<string, object>());

            return Ok(
                new Dictionary<string, object>(){
                    {"id",guid}
                }
            );
        }

        private IEnumerable<string> GetData(int dataCount)
        {
            for (int i = 0; i < dataCount; i++)
            {
                yield return "{\"num\":" + i + "}";
            }

            yield break;
        }


        [HttpGet("~/Home/Delete")]
        public IActionResult Delete(string syncId)
        {
            if (_storage.Delete(syncId))
            {
                return Ok();
            }
            else
            {
                return StatusCode(500);
            }
        }


        [HttpGet("~/Home/Page")]
        public IActionResult Page(string syncId, int page)
        {
            return File(_storage.GetStreamPage(syncId, page), "text/plain");
        }


        [HttpGet("~/Home/Page2")]
        public IActionResult Page2(string syncId, int page)
        {
            var rangeHeader = Request.Headers["Range"].ToString();
            Console.WriteLine(rangeHeader);

            var range = rangeHeader.Substring("bytes=".Length);
            var ranges = range.Split("-");

            return File(_storage2.GetStreamPage(syncId, page, long.Parse(ranges[0]), long.Parse(ranges[1])), "text/plain", true);
        }
    }
}
