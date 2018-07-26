using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Action
{
    [Serializable]
    public class QueryValueResult
    {
        public QueryValueResult(bool succeed)
        {
            Succeed = succeed;
        }

        public ILuisAction NewAction { get; set; }

        public string NewIntent { get; set; }

        public bool Succeed { get; private set; }
    }
}
