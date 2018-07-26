using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.Core.WebApiContracts.Authentication
{
    [Serializable]
	public struct AuthenticationResult
	{
		public AuthenticationResultType Type;
		public string Result;
	}

    [Serializable]
    public enum AuthenticationResultType
	{
		OK,
		AskedForAuth,
		Error
	}
}
