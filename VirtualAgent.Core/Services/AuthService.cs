using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualAgent.Core.WebApiContracts.Authentication;

namespace VirtualAgent.Core.Services
{
	public class AuthService
	{
		private AuthenticationInfo AuthenticationInfo;
		private string RedirectUrl;

		public AuthService(AuthenticationInfo authenticationInfo, string redirectUrl)
		{
			AuthenticationInfo = authenticationInfo;
			RedirectUrl = redirectUrl;
		}

		public AuthenticationResult GetAuthenticationResult()
		{
			StateClient stateClient = new StateClient(new MicrosoftAppCredentials(AuthenticationInfo.MicrosoftAppId, AuthenticationInfo.MicrosoftPassword));
			BotData userData = stateClient.BotState.GetUserData(AuthenticationInfo.Channel, AuthenticationInfo.UserId);
			string accesstoken = userData.GetProperty<string>("AccessToken");
			if(string.IsNullOrWhiteSpace(accesstoken))
			{
				return new AuthenticationResult()
				{
					Type = AuthenticationResultType.AskedForAuth,
					Result = $"https://login.microsoftonline.com/{AuthenticationInfo.AdTenant}/oauth2/authorize?client_id={AuthenticationInfo.AdClient}&response_type=code&redirect_uri={RedirectUrl}"
				};
			}

			return new AuthenticationResult()
			{
				Result = accesstoken,
				Type = AuthenticationResultType.OK
			};
		}
	}
}
