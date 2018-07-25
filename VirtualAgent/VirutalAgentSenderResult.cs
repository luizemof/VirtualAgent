using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent
{
	internal class VirutalAgentSenderResult
	{
		public VirutalAgentSenderResultType Code { get; set; }
		public string Result { get; set; }
	}

	internal enum VirutalAgentSenderResultType
	{
		OK = 0,
		NeedAuthentication = 1,
		Error = 2
	}
}
