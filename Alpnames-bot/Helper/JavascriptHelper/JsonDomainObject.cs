using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alpnames_bot.Helper.JavascriptHelper
{

    public class TopDomain
    {
        public int dont_show { get; set; }
    }

    public class FreeDomain
    {
        public string status { get; set; }
        public string domain { get; set; }
        public string tld { get; set; }
        public string currency { get; set; }
        public string type { get; set; }
        public string price_int { get; set; }
        public string price_cent { get; set; }
        public int show_top_domain { get; set; }
        public int is_in_cart { get; set; }
    }

    public class PaidDomain
    {
        public string domain { get; set; }
        public string tld { get; set; }
        public string price_int { get; set; }
        public string price_cent { get; set; }
        public string currency { get; set; }
        public string location { get; set; }
        public int is_in_cart { get; set; }
    }

    public class JsonDomainObject
    {
        public string status { get; set; }
        public int maximum_reached { get; set; }
        public TopDomain top_domain { get; set; }
        public List<FreeDomain> free_domains { get; set; }
        public List<PaidDomain> paid_domains { get; set; }
        public int current_in_cart { get; set; }
    }

}
