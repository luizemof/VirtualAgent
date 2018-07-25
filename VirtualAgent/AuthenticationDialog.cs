using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent
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
			ThumbnailCard thumbnailCard = new ThumbnailCard();
			thumbnailCard.Title = $"Hello, please sign in Azure AD";
			thumbnailCard.Subtitle = "Sign in with Azure \U0001F511";
			thumbnailCard.Buttons = new List<CardAction>();
			thumbnailCard.Buttons.Add(new CardAction()
			{
				Value = $"{RedirectUrl}",
				Type = "signin",
				Title = "Sign In"
			});
			thumbnailCard.Images = new List<CardImage>()
			{
				new CardImage(url: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcS1ODtKlKkCOVqD4AYddU1uDSLOoikXTxWCv5Aiw282LIX1_nrlWQ")
			};
			thumbnailCard.Images.Add(new CardImage());

			messageActivity.Attachments = new List<Attachment>() { thumbnailCard.ToAttachment() };

			await context.PostAsync(messageActivity);
		}
	}
}
