using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using VirtualAgent;

namespace BotApplication.Controllers
{
    public class MessageController : ApiController
    {
		public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
		{
			if(activity.Type == ActivityTypes.Message)
			{
				VirtualAgentSenderParameters parameters = new VirtualAgentSenderParameters()
				{
					Activity = activity,
					AzureAdClientId = "947fa91c-0996-467d-837e-07b17a3146c4",
					AzureAdTenantId = "5cda9700-603b-4822-8f63-3b9471b0d8b1",
					MicrosoftAppId = "af3119fb-f1fb-478e-9dd3-ebc25651381b",
					MicrosoftPassword = "ii$-D_IGtK-k^X#B"
				};

				await new VirtualAgentSender(parameters).SendAsync();
			}

			return Request.CreateResponse(HttpStatusCode.OK);
		}
    }
}
