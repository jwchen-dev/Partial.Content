using System.Net;
using System;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using System.Net.Http.Headers;

namespace Partial.Content.Tests
{
    public class PartialContentTest
    {
        private readonly HttpClient _httpClient;

        private readonly ITestOutputHelper _output;

        public PartialContentTest(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient();
        }

        [Fact]
        public async void PutToS3Test()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/PutToS3?dataCount=100&pageSize=21", UriKind.Absolute),
            };

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _output.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        [Fact]
        public async void DeleteTest()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/Delete?syncId=e857e248-43c2-4842-8eec-705e631d50b5", UriKind.Absolute),
            };

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact]
        public async void PageTest()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/Page?syncId=c641191a-4630-40bb-a590-57370b28d41b&page=0", UriKind.Absolute),
            };

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _output.WriteLine(JsonSerializer.Serialize(response.Headers));
            _output.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }


        [Fact]
        public async void Page2Test()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/Page2?syncId=c641191a-4630-40bb-a590-57370b28d41b&page=0", UriKind.Absolute),
            };
            request.Headers.Range = new RangeHeaderValue(10, 21);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _output.WriteLine(JsonSerializer.Serialize(request.Headers));
            _output.WriteLine(JsonSerializer.Serialize(response.Headers));
            _output.WriteLine(JsonSerializer.Serialize(response.Content.Headers));
            _output.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }


        [Fact]
        public async void LocalFileStreamSimpleTest()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/LocalFileStreamSimple", UriKind.Absolute),
            };

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _output.WriteLine(JsonSerializer.Serialize(response.Headers));
        }


        [Fact]
        public async void LocalFileStreamTest()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/LocalFileStream", UriKind.Absolute),
            };

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _output.WriteLine(JsonSerializer.Serialize(response.Headers));
        }


        [Fact]
        public async void LocalFileStreamTest2()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/LocalFileStream", UriKind.Absolute),
            };
            request.Headers.Range = new RangeHeaderValue(0, 100);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _output.WriteLine(JsonSerializer.Serialize(response.Content.Headers));
            _output.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }


        [Fact]
        public async void HttpClientStreamTest()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://127.0.0.1:5000/Home/HttpClientStream", UriKind.Absolute),
            };

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            _output.WriteLine(JsonSerializer.Serialize(response.Headers));
        }
    }
}
