using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alpnames_bot.Helper.JavascriptHelper
{

    public class List
    {
        public string mail_id { get; set; }
        public string mail_from { get; set; }
        public string mail_subject { get; set; }
        public string mail_excerpt { get; set; }
        public string mail_timestamp { get; set; }
        public string mail_read { get; set; }
        public string mail_date { get; set; }
        public string att { get; set; }
        public string mail_size { get; set; }
    }

    public class Stats
    {
        public string sequence_mail { get; set; }
        public int created_addresses { get; set; }
        public string received_emails { get; set; }
        public string total { get; set; }
        public string total_per_hour { get; set; }
    }

    public class Auth
    {
        public bool success { get; set; }
        public List<object> error_codes { get; set; }
    }

    public class JsonCheckEmailObject
    {
        public List<List> list { get; set; }
        public string count { get; set; }
        public string email { get; set; }
        public string alias { get; set; }
        public int ts { get; set; }
        public string sid_token { get; set; }
        public Stats stats { get; set; }
        public Auth auth { get; set; }
    }

}
