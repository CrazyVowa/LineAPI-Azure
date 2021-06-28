using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Net.Http;

namespace LineAPI_Azure
{
    public static class Global
    {
        public static string service_api_key = "1c0944a0-15e1-4a70-849b-9a4bc489842d";
        public static string service_api_secret = "07bed8ac-d84d-48d0-8434-bfa0dfb763fd";
        
    }

    public static class Generators
    {
        public static string Timestamp()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            return unixTimeMilliseconds.ToString();
        }
        public static string Nonce()
        {
            Random rand = new Random();
            string source = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string temp = "";
            for (int x = 0; x < 8; x++)
            {
                temp += source[rand.Next(62)];
            }
            return temp;
        }
        public static string Signature(string nonce, string timestamp, string secret)
        {
            string message = nonce + timestamp + "GET/v1/wallets";
            secret = secret ?? "";
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA512(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

    }
    public static class GetSignature
    {

        
        [FunctionName("GetSignature")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Generators.Timestamp();
            log.LogInformation("C# HTTP trigger function processed a request.");

            string key = req.Query["key"];
            string secret = req.Query["secret"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            key = key ?? data?.key;
            secret = secret ?? data?.secret;

            string responseMessage = "", timestamp = Generators.Timestamp(), nonce = Generators.Nonce();
            

            if(!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(secret)) //Means there is data
            {
                timestamp = Generators.Timestamp();
                nonce = Generators.Nonce();
                string signature = Generators.Signature(nonce, timestamp, secret);
                responseMessage = $"Api Key: {key}";
                responseMessage += $"\nTimestamp(UTC in Unix Time): {timestamp}";
                responseMessage += $"\nNonce: {nonce}";
                responseMessage += $"\nSignature: {signature}";
                responseMessage += "\n\nLine API Reply:\n";

                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://test-api.blockchain.line.me/v1/wallets"))
                        {
                            
                            request.Headers.TryAddWithoutValidation("service-api-key", key);
                            request.Headers.TryAddWithoutValidation("nonce", nonce);
                            request.Headers.TryAddWithoutValidation("timestamp", timestamp);
                            request.Headers.TryAddWithoutValidation("signature", signature);

                            var response = await httpClient.SendAsync(request);
                            responseMessage += response;
                            

                        }

                    }
                }
                catch
                {
                    responseMessage += "Could not connect to LINE API. Please check your internet connection.";
                }
            }
            else
            {
                responseMessage = "Line API Key and Secret is required.";
            }

            
            /*string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";*/

            return new OkObjectResult(responseMessage);
        }
    }

    public static class Function2
    {
        [FunctionName("Function2")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTPS triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }

}
