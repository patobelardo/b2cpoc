using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Identity.Client;

namespace ADHelperAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipController : ControllerBase
    {
        [HttpGet("GroupNames")]
        public async Task<ActionResult<JObject>> GetMembership(string issuerUserId)
        {
            if (string.IsNullOrEmpty(issuerUserId))
            {
                return new JObject { "You must include a userid" };
            }
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");
            IConfidentialClientApplication app;
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                            .WithClientSecret(config.ClientSecret)
                            .WithAuthority(new Uri(config.Authority))
                            .Build();
            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            JObject returnValue = new JObject();
            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired");
                Console.ResetColor();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                returnValue = await apiCaller.CallWebApiASync($"https://graph.microsoft.com/v1.0/users/{issuerUserId}/memberOf", result.AccessToken);
            }

            var groupArray = (JArray)returnValue["value"];
            var groupNames = new List<string>();
            if (groupArray != null)
            {
                foreach (JObject g in groupArray)
                {
                    var name = g["displayName"].Value<string>();
                    groupNames.Add(name);
                }
            }
            return new JsonResult(
               new
               {
                   groupNames
               });
        }

        // GET api/values
        [HttpGet("{groupid}")]
        public async Task<ActionResult<JObject>> Get(string groupid)
        {
            if (string.IsNullOrEmpty(groupid))
            {
                return new JObject { "You must include a group id" };
            }
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");
            IConfidentialClientApplication app;
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                            .WithClientSecret(config.ClientSecret)
                            .WithAuthority(new Uri(config.Authority))
                            .Build();
// With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            JObject returnValue = new JObject();
            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired");
                Console.ResetColor();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                // await apiCaller.CallWebApiAndProcessResultASync($"https://graph.microsoft.com/v1.0/groups/{groupid}", result.AccessToken, Display);
                returnValue = await apiCaller.CallWebApiASync($"https://graph.microsoft.com/v1.0/groups/{groupid}", result.AccessToken);
            }



            return returnValue;
        }
        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }
        }
        // public static string JsonPrettify(string json)
        // {
        //     using (var stringReader = new StringReader(json))
        //     using (var stringWriter = new StringWriter())
        //     {
        //         var jsonReader = new JsonTextReader(stringReader);
        //         var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
        //         jsonWriter.WriteToken(jsonReader);
        //         return stringWriter.ToString();
        //     }
        // }

        // static async Task getMembers()
        // {
        //     httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        //     Console.WriteLine("\n \n Retrieving users {0}", DateTime.Now.ToString());
        //     HttpResponseMessage response = await httpClient.GetAsync(resourceUri + "/v1.0/users?$top=5");

        //     if (response.IsSuccessStatusCode)
        //     {
        //         // Read the response and output it to the console.
        //         string users = await response.Content.ReadAsStringAsync();
        //         Console.WriteLine("\n \n Printing out Users \n \n");
        //         Console.WriteLine(JsonPrettify(users));
        //         Console.WriteLine("Received Info");
        //     }
        //     else
        //     {
        //         Console.WriteLine("Failed to retrieve To Do list\nError:  {0}\n", response.ReasonPhrase);
        //     }
        // }

        // static async Task prettyJWTPrint(String myToken)
        // {
        //     //Assume the input is in a control called txtJwtIn,
        //     //and the output will be placed in a control called txtJwtOut
        //     var jwtHandler = new JwtSecurityTokenHandler();
        //     var jwtInput = myToken;
        //     String prettyPrint = "";

        //     //Check if readable token (string is in a JWT format)
        //     var readableToken = jwtHandler.CanReadToken(jwtInput);

        //     if (readableToken != true)
        //     {
        //         Console.WriteLine("The token doesn't seem to be in a proper JWT format.");
        //     }
        //     if (readableToken == true)
        //     {
        //         var token = jwtHandler.ReadJwtToken(jwtInput);

        //         //Extract the headers of the JWT
        //         var headers = token.Header;
        //         var jwtHeader = "{";
        //         foreach (var h in headers)
        //         {
        //             jwtHeader += '"' + h.Key + "\":\"" + h.Value + "\",";
        //         }
        //         jwtHeader += "}";
        //         prettyPrint = "Header:\r\n" + JToken.Parse(jwtHeader).ToString(Formatting.Indented);

        //         //Extract the payload of the JWT
        //         var claims = token.Claims;
        //         var jwtPayload = "{";
        //         foreach (System.Security.Claims.Claim c in claims)
        //         {
        //             jwtPayload += '"' + c.Type + "\":\"" + c.Value + "\",";
        //         }
        //         jwtPayload += "}";
        //         prettyPrint += "\r\nPayload:\r\n" + JToken.Parse(jwtPayload).ToString(Formatting.Indented);

        //     }
        //     Console.WriteLine(prettyPrint);
        // }
        // static async Task getAccessToken()
        // {
        //     int retryCount = 0;
        //     bool retry = false;
        //     authContext = new AuthenticationContext(authority + tenantID);

        //     do
        //     {
        //         retry = false;
        //         try
        //         {
        //             // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
        //             result = await authContext.AcquireTokenAsync(resourceUri, clientId, new Uri(redirectUri), new PlatformParameters(PromptBehavior.Never, new CustomWebUi()));

        //             Console.Write("My Access Token : \n");
        //             await prettyJWTPrint(result.AccessToken);
        //         }
        //         catch (AdalException ex)
        //         {
        //             if (ex.ErrorCode == "temporarily_unavailable")
        //             {
        //                 retry = true;
        //                 retryCount++;
        //                 Thread.Sleep(3000);
        //             }

        //             Console.WriteLine(
        //                 String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\nRetry: {2}\n",
        //                 DateTime.Now.ToString(),
        //                 ex.ToString(),
        //                 retry.ToString()));
        //         }

        //     } while ((retry == true) && (retryCount < 3));

        //     if (result == null)
        //     {
        //         Console.WriteLine("Canceling attempt to get access token.\n");
        //         return;
        //     }

        // }

    }
}
