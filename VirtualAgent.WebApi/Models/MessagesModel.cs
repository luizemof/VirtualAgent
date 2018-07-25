using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VirtualAgent.Core.WebApiContracts.Authentication;

namespace VirtualAgent.WebApi.Models
{
	public class MessagesModel
	{
		public AuthenticationInfo AuthenticationInfo;
		public Activity Activity;
	}
}