using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// AWS Signature Version 4 signing for HTTP requests.
    /// </summary>
    public static class AwsSignatureV4
    {
        public static void Sign(HttpRequestMessage request, string accessKey, string secretKey,
            string? sessionToken, string region, string service)
        {
            var now = DateTimeOffset.UtcNow;
            var amzDate = now.ToString("yyyyMMddTHHmmssZ");
            var dateStamp = now.ToString("yyyyMMdd");

            request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
            if (!string.IsNullOrEmpty(sessionToken))
                request.Headers.TryAddWithoutValidation("x-amz-security-token", sessionToken);

            var host = request.RequestUri!.Host;
            request.Headers.Host = host;

            // Canonical request
            var httpMethod = request.Method.Method;
            var canonicalUri = request.RequestUri.AbsolutePath;
            if (string.IsNullOrEmpty(canonicalUri)) canonicalUri = "/";
            var canonicalQueryString = request.RequestUri.Query.TrimStart('?');

            var canonicalHeaders = new StringBuilder();
            var signedHeaders = new List<string> { "host", "x-amz-date" };
            if (!string.IsNullOrEmpty(sessionToken)) signedHeaders.Add("x-amz-security-token");

            canonicalHeaders.AppendLine($"host:{host}");
            canonicalHeaders.AppendLine($"x-amz-date:{amzDate}");
            if (!string.IsNullOrEmpty(sessionToken))
                canonicalHeaders.AppendLine($"x-amz-security-token:{sessionToken}");

            var canonicalHeadersStr = canonicalHeaders.ToString().TrimEnd('\n');
            var signedHeadersStr = string.Join(";", signedHeaders.OrderBy(h => h));

            string payloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // empty body hash
            if (request.Content is not null)
            {
                var bodyBytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                payloadHash = ToHex(SHA256.HashData(bodyBytes));
            }

            var canonicalRequest = $"{httpMethod}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeadersStr}\n\n{signedHeadersStr}\n{payloadHash}";

            // String to sign
            var algorithm = "AWS4-HMAC-SHA256";
            var credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
            var stringToSign = $"{algorithm}\n{amzDate}\n{credentialScope}\n{ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";

            // Signing key
            var kDate = HmacSHA256(Encoding.UTF8.GetBytes("AWS4" + secretKey), dateStamp);
            var kRegion = HmacSHA256(kDate, region);
            var kService = HmacSHA256(kRegion, service);
            var kSigning = HmacSHA256(kService, "aws4_request");

            var signature = ToHex(HmacSHA256(kSigning, stringToSign));

            var authHeader = $"{algorithm} Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeadersStr}, Signature={signature}";
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        static byte[] HmacSHA256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
