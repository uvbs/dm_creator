using Alpnames_bot.Helper.ObjectHelper;
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
                value = GetJsonEmailObject(json);
            }
            catch (Exception ex)
            {
                ex.ToString();
                throw;
            }

            return value;
        }

        private static string GetJsonEmailObject(string json)
        {
            string email = string.Empty;
            var result = JsonConvert.DeserializeObject<JsonEmailObject>(json);
            if (result != null && result.result != null && result.result.list != null && result.result.list.Count > 0)
                email = result.result.list[0].mail_recipient;
            return email;
        }

        public static JsonDomainObject GetJsonDomainObject(string json)
        {
            return JsonConvert.DeserializeObject<JsonDomainObject>(json);
               
        }
    }
}
