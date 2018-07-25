using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Primitives;
using System;

namespace VirtualAgent.Functions
{
	public static class Authentication
	{
		private class HeaderArgs
		{
			public StringValues MicrosoftAppId;
			public StringValues MicrosoftPassword;
			public StringValues Channel;
			public StringValues AdTenant;
			public StringValues AdClient;
			public StringValues UserId;
		}

		private struct AuthenticationResult
		{
			public int Code;
			public string Result;

			public AuthenticationResult(int code, string result = null)
			{
				Code = code;
				Result = result;
			}
		}

		[FunctionName("Authentication")]
		public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
		{
			log.Info("Request Authentication");

			try
			{
				log.Info("Get access token");
				HeaderArgs args = ValidateHeadersArgs(req);
				StateClient stateClient = new StateClient(new MicrosoftAppCredentials(args.MicrosoftAppId, args.MicrosoftPassword));
				BotData userData = stateClient.BotState.GetUserData(args.Channel, args.UserId);
				string accesstoken = userData.GetProperty<string>("AccessToken");
				UriBuilder rediretUri = new UriBuilder()
				{
					Host = req.Host.Host,
					Scheme = req.Scheme,
					Port = req.Host.Port ?? default(int),
					Path = "api/Authenticated"
				};

				AuthenticationResult result = new AuthenticationResult(0, accesstoken);
				if (string.IsNullOrEmpty(accesstoken))
				{
					log.Info("Empty access token");
					string url = $"https://login.microsoftonline.com/{args.AdTenant}/oauth2/authorize?client_id={args.AdClient}&response_type=code&redirect_uri={rediretUri}";
					result = new AuthenticationResult(1, url);
				}

				return new OkObjectResult(result);
			}
			catch (Exception e)
			{
				log.Error("An error has occurred");
				return new BadRequestObjectResult(new AuthenticationResult(3, e.Message));
			}
		}

		private static HeaderArgs ValidateHeadersArgs(HttpRequest req)
		{
			HeaderArgs headerArgs = new HeaderArgs();
			if (!req.Headers.TryGetValue("microsoftAppId", out headerArgs.MicrosoftAppId))
				throw new Exception("Missing microsoftAppId");

			if (!req.Headers.TryGetValue("microsoftPassword", out headerArgs.MicrosoftPassword))
				throw new Exception("Missing microsoftPassword");

			if (!req.Headers.TryGetValue("channel", out headerArgs.Channel))
				throw new Exception("Missing channel");

			if (!req.Headers.TryGetValue("adTenant", out headerArgs.AdTenant))
				throw new Exception("Missing adTenant");

			if (!req.Headers.TryGetValue("adClient", out headerArgs.AdClient))
				throw new Exception("Missing adClient");

			if (!req.Headers.TryGetValue("userId", out headerArgs.UserId))
				throw new Exception("Missing userId");

			return headerArgs;
		}
	}
}
