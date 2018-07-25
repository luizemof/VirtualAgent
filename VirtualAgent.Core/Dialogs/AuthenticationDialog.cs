using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.Core.Dialogs
{
	[Serializable]
	public class AuthenticationDialog : IDialog
	{
		private string RedirectUrl;

		public AuthenticationDialog(string redirectUrl)
		{
			RedirectUrl = redirectUrl;
		}

		public async Task StartAsync(IDialogContext context)
		{
			IMessageActivity messageActivity = context.MakeMessage();
			ThumbnailCard thumbnailCard = new ThumbnailCard
			{
				Title = $"Hello, please sign in Azure AD",
				Subtitle = "Sign in with Azure \U0001F511",
				Buttons = new List<CardAction>()
				{
					new CardAction()
					{
						Value = $"{RedirectUrl}",
						Type = "signin",
						Title = "Sign In"
					}
				},
				Images = new List<CardImage>()
				{
					new CardImage(url: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcS1ODtKlKkCOVqD4AYddU1uDSLOoikXTxWCv5Aiw282LIX1_nrlWQ")
				}
			};

			messageActivity.Attachments = new List<Attachment>() { thumbnailCard.ToAttachment() };
			await context.PostAsync(messageActivity);
		}
	}
}
