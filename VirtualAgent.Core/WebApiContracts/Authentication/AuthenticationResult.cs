using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.Core.WebApiContracts.Authentication
{
	public struct AuthenticationResult
	{
		public AuthenticationResultType Type;
		public string Result;
	}

	public enum AuthenticationResultType
	{
		OK,
		AskedForAuth,
		Error
	}
}
