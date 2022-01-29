using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SynoAI.Models;
using SynoAI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SynoAI.Notifiers.Webhook
{
    /// <summary>
    /// Calls a third party API.
    /// </summary>
    public class Webhook : NotifierBase
    {
        /// <summary>
        /// The URL to send the request to.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// The HTTP method (POST/PUT/etc).
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// The type of authentication.
        /// </summary>
        public AuthorizationMethod Authentication { get; set; }
        /// <summary>
        /// The username when using Basic authentication.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// The password to use when using Basic authentication.
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// The token to use when using Bearer authentication.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// The field name when posting the image.
        /// </summary>
        public string Field { get; set; }
        /// <summary>
        /// Whether the image should be sent in POST/PUT/PATCH requests. When this property is true, the request will made using 
        /// content-type of multipart/form-data.
        /// </summary>
        public bool SendImage { get; set; }

        /// <summary>
        /// Sends a notification to the Webhook.
        /// </summary>
        /// <param name="camera">The camera that triggered the notification.</param>
        /// <param name="processedImage">Object for fetching the processed image.</param>
        /// <param name="foundTypes">The list of types that were found.</param>
        /// <param name="logger">A logger.</param>
        public override async Task SendAsync(Camera camera, ProcessedImage processedImage, IEnumerable<string> foundTypes, ILogger logger)
        {
            logger.LogInformation($"{camera.Name}: Webhook: Processing");
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = GetAuthenticationHeader();

                MultipartFormDataContent form = new MultipartFormDataContent();
                //form.Add(new StringContent(device), "\"device\"");
                //form.Add(new StringContent(message), "\"message\"");
                //form.Add(new StringContent(((int)Priority).ToString()), "\"priority\"");
                //form.Add(new StringContent(Sound ?? String.Empty), "\"sound\"");
                //form.Add(new StringContent(ApiKey), "\"token\"");
                //form.Add(new StringContent(UserKey), "\"user\"");
                //form.Add(new StringContent(title), "\"title\"");

                if (SendTypes)
                {
                    form.Add(JsonContent.Create(foundTypes));
                }

                FileStream fileStream = null;
                switch (Method)
                {
                    case "PATCH":
                    case "POST":
                    case "PUT":
                        if (SendImage)
                        {
                            fileStream = processedImage.GetReadonlyStream();
                            form.Add(new StreamContent(fileStream), Field, processedImage.FileName);
                        }
                        break;
                }

                logger.LogInformation($"{camera.Name}: Webhook: Calling {Method}.");

                HttpResponseMessage message;
                switch (Method)
                {
                    case "DELETE":
                        message = await client.DeleteAsync(Url);
                        break;
                    case "GET":
                        message = await client.GetAsync(Url);
                        break;
                    case "PATCH":
                        message = await client.PatchAsync(Url, form);
                        break;
                    case "POST":
                        message = await client.PostAsync(Url, form);
                        break;
                    case "PUT":
                        message = await client.PutAsync(Url, form);
                        break;
                    default:
                        logger.LogError($"{camera.Name}: Webhook: The method type '{Method}' is not supported.");
                        return;
                }

                if (message.IsSuccessStatusCode)
                {
                    logger.LogInformation($"{camera.Name}: Webhook: Success.");
                }
                else
                {
                    logger.LogWarning($"{camera.Name}: Webhook: The end point responded with HTTP status code '{message.StatusCode}'.");
                }

                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Generates an authentication header for the client.
        /// </summary>
        /// <returns>An authentication header.</returns>
        private AuthenticationHeaderValue GetAuthenticationHeader()
        {
            string parameter;
            switch (Authentication)
            {
                case AuthorizationMethod.Basic:
                    byte[] bytes = Encoding.ASCII.GetBytes($"{Username}:{Password}");
                    parameter = Convert.ToBase64String(bytes);
                    break;
                case AuthorizationMethod.Bearer:
                    parameter = Token;
                    break;
                default:
                    return null;
            }

            return new AuthenticationHeaderValue(Authentication.ToString(), parameter);
        }
    }
}
