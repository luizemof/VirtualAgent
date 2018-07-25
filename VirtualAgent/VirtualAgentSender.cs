using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent
{
	public class VirtualAgentSender
	{
		private VirtualAgentSenderParameters Parameters;

		public VirtualAgentSender(VirtualAgentSenderParameters parameters)
		{
			Parameters = parameters;
			ValidateParameters();
		}

		public async Task SendAsync()
		{
			VirutalAgentSenderResult result = null;
			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("microsoftAppId", Parameters.MicrosoftAppId);
				client.DefaultRequestHeaders.Add("microsoftPassword", Parameters.MicrosoftPassword);
				client.DefaultRequestHeaders.Add("channel", "skype"); // Parameters.Activity.ChannelId
				client.DefaultRequestHeaders.Add("adTenant", Parameters.AzureAdTenantId);
				client.DefaultRequestHeaders.Add("adClient", Parameters.AzureAdClientId);
				client.DefaultRequestHeaders.Add("userId", Parameters.Activity.From.Id);

#if DEBUG
				HttpResponseMessage response = await client.GetAsync("http://localhost:7071/api/Authentication");
#else
				HttpResponseMessage response = await client.GetAsync("http://localhost:7071/api/Authentication");
#endif
				result = JsonConvert.DeserializeObject<VirutalAgentSenderResult>(await response.Content.ReadAsStringAsync());
			}

			if (result.Code == VirutalAgentSenderResultType.NeedAuthentication)
				await Conversation.SendAsync(Parameters.Activity, () => new AuthenticationDialog(result.Result));
		}

		private void ValidateParameters()
		{
			Parameters.ThrowIfNull(nameof(Parameters));
			Parameters.Activity.ThrowIfNull(nameof(Parameters.Activity));
			Parameters.AzureAdClientId.ThrowIfNull(nameof(Parameters.AzureAdClientId));
			Parameters.AzureAdTenantId.ThrowIfNull(nameof(Parameters.AzureAdTenantId));
			Parameters.MicrosoftAppId.ThrowIfNull(nameof(Parameters.MicrosoftAppId));
			Parameters.MicrosoftPassword.ThrowIfNull(nameof(Parameters.MicrosoftPassword));
		}
	}

	public struct VirtualAgentSenderParameters
	{
		public string MicrosoftAppId { get; set; }
		public string MicrosoftPassword { get; set; }
		public string AzureAdTenantId { get; set; }
		public string AzureAdClientId { get; set; }
		public Activity Activity { get; set; }
	}
}
