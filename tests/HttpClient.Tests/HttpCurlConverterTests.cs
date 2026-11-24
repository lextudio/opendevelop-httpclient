using System.Net.Http;
using OpenDevelop.HttpClient;
using Xunit;

namespace OpenDevelop.HttpClient.Tests
{
    public class HttpCurlConverterTests
    {
        [Fact]
        public void CurlToHttp_SimpleGet_ParsesCorrectly()
        {
            var curl = "curl https://example.com/api";
            var http = HttpCurlConverter.CurlToHttp(curl);
            Assert.Contains("GET", http);
            Assert.Contains("https://example.com/api", http);
        }

        [Fact]
        public void CurlToHttp_WithHeaders_IncludesHeaders()
        {
            var curl = "curl -H \"Accept: application/json\" https://example.com/api";
            var http = HttpCurlConverter.CurlToHttp(curl);
            Assert.Contains("Accept: application/json", http);
        }

        [Fact]
        public void CurlToHttp_WithData_SetsPostMethod()
        {
            var curl = "curl -d '{\"key\":\"value\"}' https://example.com/api";
            var http = HttpCurlConverter.CurlToHttp(curl);
            Assert.Contains("POST", http);
        }

        [Fact]
        public void HttpToCurl_SimpleGet_GeneratesCurl()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var curl = HttpCurlConverter.HttpToCurl(request);
            Assert.Contains("curl", curl);
            Assert.Contains("https://example.com/api", curl);
        }
    }
}
