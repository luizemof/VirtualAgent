using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using VirtualAgent.Core.Dialogs;
using VirtualAgent.Core.Services;
using VirtualAgent.Core.WebApiContracts.Authentication;
using VirtualAgent.WebApi.Models;

namespace VirtualAgent.WebApi.Controllers
{
	public class MessagesController : ApiController
    {
		[HttpPost]
		public async Task<HttpResponseMessage> Post()
		{
			try
			{
				string body = await Request.Content.ReadAsStringAsync();
				if (string.IsNullOrWhiteSpace(body))
					throw new ArgumentException(nameof(body));

				MessagesModel messageModel = JsonConvert.DeserializeObject<MessagesModel>(body);
				AuthenticationResult result = new AuthService(messageModel.AuthenticationInfo, GetBaseUri("api/Messages/Authenticated").AbsoluteUri).GetAuthenticationResult();
				switch (result.Type)
				{
					case AuthenticationResultType.OK:
						break;
					case AuthenticationResultType.AskedForAuth:
						await Conversation.SendAsync(messageModel.Activity, () => new AuthenticationDialog(result.Result));
						break;
					case AuthenticationResultType.Error:
						break;
					default:
						break;
				}

				return Request.CreateResponse(HttpStatusCode.Accepted);
			}
			catch (Exception e)
			{
				return Request.CreateResponse(HttpStatusCode.BadRequest, e);
			}
		}

		[HttpGet]
		[ActionName("Authenticated")]
		public HttpResponseMessage Get()
		{
			try
			{
				return Request.CreateResponse(HttpStatusCode.OK, "Authenticated.");
			}
			catch (Exception e)
			{
				return Request.CreateResponse(HttpStatusCode.BadRequest, e);
			}
		}

		private Uri GetBaseUri(string path)
		{
			return new UriBuilder()
			{
				Host = Request.RequestUri.Host,
				Scheme = Request.RequestUri.Scheme,
				Port = Request.RequestUri.Port,
				Path = path
			}.Uri;
		}
	}
}
