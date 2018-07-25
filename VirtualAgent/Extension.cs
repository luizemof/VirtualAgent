using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent
{
	public static class Extension
	{
		public static void ThrowIfNull(this object obj, string nameof)
		{
			if (obj == null || (obj is string && string.IsNullOrEmpty((string)obj)))
				throw new ArgumentException(nameof);
		}
	}
}
