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

    }
}
