using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alpnames_bot.Helper.ObjectHelper
{
    class JsonEmailObject
    {
        public string uid { get; set; }
        public string usr { get; set; }
        public string api_token { get; set; }
        public string ryo_url { get; set; }
        public bool hasFlash { get; set; }
        public string ZeroSwf { get; set; }
        public string passwordJs { get; set; }
        public string ryoJs { get; set; }
        public string bitcoinJs { get; set; }
        public string recaptchaJs { get; set; }
        public string recaptcha2Js { get; set; }
        public string @base { get; set; }
        public string tab { get; set; }
        public string assets { get; set; }
        public string lang { get; set; }
        public string ajax_url { get; set; }
        public Result result { get; set; }
        public string email_addr { get; set; }
        public string alias { get; set; }
        public bool use_alias { get; set; }
        public int email_timestamp { get; set; }
        public string domain { get; set; }
        public string site { get; set; }
        public int limit { get; set; }
        public string display_host { get; set; }
        public string recaptcha_pub { get; set; }
        public string row_template { get; set; }
        public string stats_template { get; set; }
        public string no_emails_template { get; set; }
        public string email_template { get; set; }
        public string file_list_template { get; set; }
        public string att_template { get; set; }
        public string att_file_template { get; set; }
        public bool change_logo_on { get; set; }
    }

    public class List
    {
        public object mail_id { get; set; }
        public string mail_from { get; set; }
        public string mail_subject { get; set; }
        public string mail_excerpt { get; set; }
        public object mail_timestamp { get; set; }
        public object mail_read { get; set; }
        public string mail_date { get; set; }
        public object att { get; set; }
        public string mail_size { get; set; }
        public string reply_to { get; set; }
        public string content_type { get; set; }
        public string mail_recipient { get; set; }
        public int? source_id { get; set; }
        public int? source_mail_id { get; set; }
        public string mail_body { get; set; }
        public int? size { get; set; }
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

    public class Result
    {
        public List<List> list { get; set; }
        public string count { get; set; }
        public string email { get; set; }
        public string alias { get; set; }
        public int ts { get; set; }
        public string sid_token { get; set; }
        public Stats stats { get; set; }

    }
}
