using Alpnames_bot.Helper.ObjectHelper;
using HtmlAgilityPack;
using Jurassic.Library;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alpnames_bot.Helper.JavascriptHelper
{
    class JsonOperator
    {

        public static long GetEmailIdFromEmailsRead(string json)
        {
            long value = -1;

            try
            {

                JsonCheckEmailObject result = JsonConvert.DeserializeObject<JsonCheckEmailObject>(json);
                if(result != null && result.list != null)
                {
                    foreach(var r in result.list)
                    {
                        if(r.mail_subject.ToLower().Contains("freenom".ToLower()))
                        {
                            long.TryParse( r.mail_id, out value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
                throw;
            }

            return value;
        }

        public static string GetJsonValueForKey(string html, string key)
        {
            string value = string.Empty;

            try
            {
                // Getting json string from variable
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);
                var script = doc.DocumentNode.Descendants()
                                             .Where(n => n.Name == "script")
                                             .First().InnerText;

                // Return the data of spect and stringify it into a proper JSON object
                var engine = new Jurassic.ScriptEngine();
                var result = engine.Evaluate("(function() { " + script + " return gm_init_vars; })()");
                var json = JSONObject.Stringify(engine, result);

                // Json parsing
                value = GetJsonObjectValue(json, key);
            }
            catch (Exception ex)
            {
                ex.ToString();
                throw;
            }

            return value;
        }

        private static string GetJsonObjectValue(string json, string key)
        {
            string value = string.Empty;
            var result = JsonConvert.DeserializeObject<JsonEmailObject>(json);
            if (key.Equals(StringHelper.Constants.EmailKey))
            {
                if (result != null && result.result != null && result.result.list != null && result.result.list.Count > 0)
                    value = result.result.list[0].mail_recipient;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                string em = result.email_addr;
                string[] splitEm = em.Split(new char[] { '@' });
                if(splitEm?.Count() > 0)
                {
                    value = splitEm[0];
                }
            }
            if (key.Equals(StringHelper.Constants.ApiKey))
            {
                if (!string.IsNullOrWhiteSpace(result.api_token))
                    value = result.api_token;
            }
            return value;
        }

        public static JsonDomainObject GetJsonDomainObject(string json)
        {
            return JsonConvert.DeserializeObject<JsonDomainObject>(json);

        }
    }
}
