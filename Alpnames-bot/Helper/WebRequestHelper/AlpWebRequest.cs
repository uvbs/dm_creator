using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Alpnames_bot.Helper
{
    class AlpWebRequest
    {

        private CookieContainer cookieContainer = new CookieContainer();
        private CookieContainer myAlpCookieContainer = new CookieContainer();

        private DataTable dtRecords;
        private int index;
        private string proxyIpParam;
        private string domain;
        private string ns1;
        private string ns2;
        private string ns3;
        private CancellationToken cancellationToken;
        private string username;
        private string password;
        private string responseUri;
        private string searchString;
        private string orderid;
        private string contactid;
        private string adminContactid;
        private string billingContactid;
        private string techContactid;
        private string csrfToken;
        private string dns1;
        private string dns2;

        private string proxy { get; set; }

        enum RandomValuePicker
        {
            Name,
            Address,
            Organisation,
            Email,
            City,
            ZipCode,
            Phone,
            Region,
            Country
        }

        public AlpWebRequest()
        {

        }

        public AlpWebRequest(DataTable dtRecords, string username, string password, int index, string proxyIpParam, string domain, string ns1, string ns2, string ns3,
            string searchString, CancellationToken cancellationToken)
        {
            this.dtRecords = dtRecords;
            this.username = username;
            this.password = password;
            this.index = index;
            this.proxyIpParam = proxyIpParam;
            this.domain = domain;
            this.ns1 = ns1;
            this.ns2 = ns2;
            this.ns3 = ns3;
            this.cancellationToken = cancellationToken;
            this.searchString = searchString;
        }

        public AlpWebRequest(DataTable dtRecords, int index, string domain, string dns1, string dns2, CancellationToken cancellationToken)
        {
            this.dtRecords = dtRecords;
            this.index = index;
            this.domain = domain;
            this.dns1 = dns1;
            this.dns2 = dns2;
            this.cancellationToken = cancellationToken;
        }

        private void AddIfProxyEnabled(HttpWebRequest request)
        {
            if (string.IsNullOrWhiteSpace(proxy))
                return;
            string[] strSplitProxy = proxy.Split(':');
            if (strSplitProxy.Length == 1 && !string.IsNullOrWhiteSpace(strSplitProxy[0]))
            {
                string part1 = strSplitProxy[0].Trim();
                request.Proxy = new WebProxy(part1);
            }
            else if (strSplitProxy.Length == 2 && !string.IsNullOrWhiteSpace(strSplitProxy[0]) && !string.IsNullOrWhiteSpace(strSplitProxy[1]))
            {
                string part1 = strSplitProxy[0].Trim();
                string part2 = strSplitProxy[1].Trim();
                int port = 0;
                if (int.TryParse(part2, out port))
                    request.Proxy = new WebProxy(part1, port);

            }
        }

        public void MakeRequests()
        {
            try
            {
                HttpWebResponse response;
                string responseText = string.Empty;

                CheckIfCancellationRequested();

                if (Request_securelogin_org(out response))
                {
                    responseText = ReadResponse(response);
                    responseUri = response.ResponseUri.AbsoluteUri;
                    response.Close();
                }

                CheckIfCancellationRequested();


                if (Request_alpnames_com(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();


                if (Request_alpnames_com_1(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(responseText);
                IEnumerable<HtmlNode> htmlNode = htmlDocument.DocumentNode.Descendants("li");
                foreach (var el in htmlNode)
                {
                    if (el.Attributes != null && el.Attributes["class"] != null)
                    {
                        string val = el.Attributes["class"].Value;
                        if (val.Equals("user-opt", StringComparison.OrdinalIgnoreCase))
                        {
                            if (el.InnerText.ToLower().Contains("welcome".ToLower()))
                            {
                                dtRecords.Rows[index]["status"] = "Login successful...";
                                break;
                            }
                        }
                        else
                        {
                            dtRecords.Rows[index]["status"] = "Login failed...";
                            return;
                        }
                    }
                }

                //if (Request_my_alpnames_com_getCsrf(out response))
                //{
                //    responseText = ReadResponse(response);
                //    for (int i = 0; i < response.Headers.Count; ++i)
                //    {
                //        string header = response.Headers.GetKey(i);
                //        foreach (string value in response.Headers.GetValues(i))
                //        {
                //            //Console.WriteLine("{0}: {1}", header, value);
                //            csrfToken = value;
                //        }
                //    }
                //    response.Close();
                //}

                CheckIfCancellationRequested();


                if (Request_alpnames_com_to(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(responseText);
                HtmlNode htmlNode1 = htmlDocument.DocumentNode.Descendants("a").FirstOrDefault();
                string scriptVal = string.Empty;
                if (htmlNode1.Attributes != null && htmlNode1.Attributes.Count > 0)
                    scriptVal = htmlNode1.Attributes[0].Value;

                dtRecords.Rows[index]["status"] = "Identifying details for processing...";

                CheckIfCancellationRequested();

                if (Request_my_alpnames_com(out response, scriptVal))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                //referrer url
                responseUri = response.ResponseUri.AbsoluteUri;
                string loginUserId = string.Empty;
                string url = string.Empty;
                string langPref = string.Empty;
                string role = string.Empty;


                htmlDocument.LoadHtml(responseText);
                IEnumerable<HtmlNode> nodes = htmlDocument.DocumentNode.Descendants("input");
                if (nodes != null)
                {
                    foreach (HtmlNode nd in nodes)
                    {
                        if (nd.Attributes != null && nd.Attributes.Count > 0)
                        {
                            if (nd.Attributes["name"] != null)
                            {
                                if (nd.Attributes["name"].Value == "userLoginId")
                                {
                                    loginUserId = nd.Attributes["value"].Value;
                                }
                                else if (nd.Attributes["name"].Value == "url")
                                {
                                    url = nd.Attributes["value"].Value;
                                }
                                else if (nd.Attributes["name"].Value == "role")
                                {
                                    role = nd.Attributes["value"].Value;
                                }
                                else if (nd.Attributes["name"].Value == "langpref")
                                {
                                    langPref = nd.Attributes["value"].Value;
                                }
                            }

                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(loginUserId))
                    dtRecords.Rows[index]["status"] = "Retrieved login details successfully...";
                else
                {
                    dtRecords.Rows[index]["status"] = "Error retrieving login details...";
                    return;
                }
                CheckIfCancellationRequested();


                if (Request_my_alpnames_com_2(out response, responseUri, loginUserId, url, langPref, role))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                //if (Request_my_alpnames_com_assServlet(out response))
                //{
                //    responseText = ReadResponse(response);

                //    response.Close();
                //}

                //if (Request_my_alpnames_com_dexServlet(out response))
                //{
                //    responseText = ReadResponse(response);

                //    response.Close();
                //}

                //if (Request_my_alpnames_com_iptServlet(out response))
                //{
                //    responseText = ReadResponse(response);

                //    response.Close();
                //}

                dtRecords.Rows[index]["status"] = "Getting cross site tokens...";

                CheckIfCancellationRequested();

                if (Request_my_alpnames_com_getCsrf(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                // getting csrf token
                htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(responseText);
                string script = responseText;

                if (!string.IsNullOrWhiteSpace(script))
                {
                    List<string> lst = script.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                        .Where(x => x.ToLower().Contains("setRequestHeader".ToLower()) && x.ToLower().Contains("OWASP_CSRFTOKEN".ToLower())).ToList();
                    if (lst != null && lst.Count > 0)
                    {
                        string line = lst[0];
                        string[] splits = line.Split(',');
                        if (splits != null && splits.Count() > 1)
                        {
                            string secondSplit = splits[1];
                            string csrftok = string.Empty;
                            bool firstQuoteFound = false;
                            foreach (char ch in secondSplit)
                            {
                                if (ch == '"' && !firstQuoteFound)
                                {
                                    firstQuoteFound = true;
                                    continue;
                                }
                                else if (ch == '"' && firstQuoteFound)
                                {
                                    break;
                                }
                                csrftok += ch;
                            }
                            csrfToken = csrftok;
                        }
                    }

                }

                if (!string.IsNullOrWhiteSpace(csrfToken))
                {
                    dtRecords.Rows[index]["status"] = "Cross site tokens retrieved...";
                }
                else
                {
                    dtRecords.Rows[index]["status"] = "Error retrieving cross site tokens...";
                    return;
                }

                dtRecords.Rows[index]["status"] = "Fetching oreder id...";
                CheckIfCancellationRequested();

                if (Request_my_alpnames_com_FetchOrderId(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                // getting order id
                htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(responseText);
                IEnumerable<HtmlNode> orderIdElements = htmlDocument.DocumentNode.Descendants("input");
                if (orderIdElements != null)
                {
                    foreach (HtmlNode el in orderIdElements)
                    {
                        if (el.Attributes != null && el.Attributes.Count > 0)
                        {
                            if (el.Attributes["name"] != null && el.Attributes["name"].Value.Equals("orderId", StringComparison.OrdinalIgnoreCase))
                            {

                                orderid = el.Attributes["value"].Value;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(orderid))
                {
                    dtRecords.Rows[index]["status"] = "Order id retrieved...";
                }
                else
                {
                    dtRecords.Rows[index]["status"] = "Error retrieving oreder id...";
                    return;
                }
                CheckIfCancellationRequested();

                if (Request_my_alpnames_com_getRefKey(out response))
                {
                    responseText = ReadResponse(response);
                    responseUri = response.ResponseUri.AbsoluteUri;
                    response.Close();
                }

                dtRecords.Rows[index]["status"] = "Retrieving contact id...";

                CheckIfCancellationRequested();

                if (Request_my_alpnames_com_getContactId(out response))
                {
                    responseText = ReadResponse(response);
                    //responseUri = response.ResponseUri.AbsoluteUri;
                    response.Close();
                }

                //get contact id
                #region contactid
                htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(responseText);
                orderIdElements = htmlDocument.DocumentNode.Descendants("input");//.Where(node => node.Name == "registrantcontactid");
                if (orderIdElements != null)
                {
                    foreach (HtmlNode el in orderIdElements)
                    {
                        if (el.Attributes != null && el.Attributes.Count > 0)
                        {
                            if (el.Attributes["name"] != null && el.Attributes["name"].Value.Equals("registrantcontactid", StringComparison.OrdinalIgnoreCase))
                            {

                                contactid = el.Attributes["value"].Value;
                            }
                        }
                    }
                }
                orderIdElements = htmlDocument.DocumentNode.Descendants("input");//.Where(node => node.Name == "admincontactid");
                if (orderIdElements != null)
                {
                    foreach (HtmlNode el in orderIdElements)
                    {
                        if (el.Attributes != null && el.Attributes.Count > 0)
                        {
                            if (el.Attributes["name"] != null && el.Attributes["name"].Value.Equals("admincontactid", StringComparison.OrdinalIgnoreCase))
                            {

                                adminContactid = el.Attributes["value"].Value;
                            }
                        }
                    }
                }
                orderIdElements = htmlDocument.DocumentNode.Descendants("input");//.Where(node => node.Name == "billingcontactid");
                if (orderIdElements != null)
                {
                    foreach (HtmlNode el in orderIdElements)
                    {
                        if (el.Attributes != null && el.Attributes.Count > 0)
                        {
                            if (el.Attributes["name"] != null && el.Attributes["name"].Value.Equals("billingcontactid", StringComparison.OrdinalIgnoreCase))
                            {

                                billingContactid = el.Attributes["value"].Value;
                            }
                        }
                    }
                }
                orderIdElements = htmlDocument.DocumentNode.Descendants("input");//.Where(node => node.Name == "techcontactid");
                if (orderIdElements != null)
                {
                    foreach (HtmlNode el in orderIdElements)
                    {
                        if (el.Attributes != null && el.Attributes.Count > 0)
                        {
                            if (el.Attributes["name"] != null && el.Attributes["name"].Value.Equals("techcontactid", StringComparison.OrdinalIgnoreCase))
                            {

                                techContactid = el.Attributes["value"].Value;
                            }
                        }
                    }
                }
                #endregion

                if (!string.IsNullOrWhiteSpace(contactid))
                {
                    dtRecords.Rows[index]["status"] = "Contact id retrieved...";
                }
                else
                {
                    dtRecords.Rows[index]["status"] = "Error retrieving oreder id...";
                    return;
                }
                CheckIfCancellationRequested();

                //Request_my_alpnames_com_openUpdatenameserverWindow
                if (Request_my_alpnames_com_openUpdatenameserverWindow(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                dtRecords.Rows[index]["status"] = "Updating nameservers...";

                CheckIfCancellationRequested();

                if (Request_my_alpnames_com_updateNameservers(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                dtRecords.Rows[index]["status"] = "Updating address...";
                CheckIfCancellationRequested();


                if (Request_my_alpnames_com_updateAddress(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }
                dtRecords.Rows[index]["status"] = "Done...";

            }
            catch (OperationCanceledException)
            {
                dtRecords.Rows[index]["status"] = "Cancelled...";
            }
            catch (Exception)
            {
                dtRecords.Rows[index]["status"] = "Error occured...";
            }

        }

        private void CheckIfCancellationRequested()
        {
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();
        }

        private static string ReadResponse(HttpWebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            {
                Stream streamToRead = responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                {
                    streamToRead = new GZipStream(streamToRead, CompressionMode.Decompress);
                }
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                {
                    streamToRead = new DeflateStream(streamToRead, CompressionMode.Decompress);
                }

                using (StreamReader streamReader = new StreamReader(streamToRead, Encoding.UTF8))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        #region navigating to my.alpnames.com
        private bool Request_alpnames_com_to(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://alpnames.com/content.php?action=cp_login");
                //request.AllowAutoRedirect = true;
                request.CookieContainer = cookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Referer = "http://alpnames.com/";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; customer_preferred_display_currency=INR; selected_lang=en; custemail=adytech2010%40gmail.com; custname=Pierre+Demarque; custid=14249633; pnctest=1; fc_vid=visitor33048704934; PHPSESSID=31pfeivt39tunl7kb78hghvav1; _gat=1; _dc_gtm_UA-53818056-1=1; fc_g=%7B%22session_geo%22%3A%22%7B%5C%22locale%5C%22%3A%7B%5C%22country%5C%22%3A%5C%22us%5C%22%2C%5C%22lang%5C%22%3A%5C%22en%5C%22%7D%2C%5C%22current_session%5C%22%3A%7B%5C%22url%5C%22%3A%5C%22http%3A%2F%2Falpnames.com%2F%5C%22%7D%2C%5C%22browser%5C%22%3A%7B%5C%22browser%5C%22%3A%5C%22Firefox%5C%22%2C%5C%22version%5C%22%3A44%2C%5C%22os%5C%22%3A%5C%22Windows%5C%22%7D%2C%5C%22device%5C%22%3A%7B%5C%22is_tablet%5C%22%3Afalse%2C%5C%22is_phone%5C%22%3Afalse%2C%5C%22is_mobile%5C%22%3Afalse%7D%7D%22%2C%22location%22%3A%22%7B%5C%22latitude%5C%22%3A%5C%2226.850000%5C%22%2C%5C%22longitude%5C%22%3A%5C%2280.916702%5C%22%2C%5C%22address%5C%22%3A%7B%5C%22city%5C%22%3A%5C%22Lucknow%5C%22%2C%5C%22region%5C%22%3A%5C%22Uttar%20Pradesh%5C%22%2C%5C%22country%5C%22%3A%5C%22India%5C%22%2C%5C%22country_code%5C%22%3A%5C%22IN%5C%22%7D%2C%5C%22source%5C%22%3A%5C%22google%5C%22%2C%5C%22ipAddress%5C%22%3A%5C%22106.219.14.158%5C%22%2C%5C%22version%5C%22%3A0.4%7D%22%7D; userloggedin=yes");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com(out HttpWebResponse response, string url)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Referer = "http://alpnames.com/content.php?action=cp_login";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=1A635D2A76CBD7F26194CCE32C140428-n3; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_2(out HttpWebResponse response, string referrer, string loginUserId, string url,
            string langPref, string role)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/AuthenticationPassServlet");
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=B23D742DC76081E6A886D41D5C0613DF-n2; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;
                request.ContentType = "application/x-www-form-urlencoded";

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = "userLoginId=" + loginUserId + "&url=" + url + "&langpref=" + langPref + "&role=" + role;
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;

        }

        private bool Request_my_alpnames_com_assServlet(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/AuthenticationPassServlet");

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=FAA6DF141E97E05FEB264F0D6B709780-n3; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;
                request.ContentType = "application/x-www-form-urlencoded";

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"userLoginId=M9digAZ7pzeXHmPewnIdQAqGr21xmqXItVeXRhE4GeZSr4DeSCqVblvPYvDdWH8uLgSWUdjHMnYExJE8D1kO4plcXSlZu763yP2TQa0a2oECiIOEneVbfWUoFsmo9JWJ&url=D6sx6IOop3YBQDDexNmIHzNzfnmdLX6pXxUZZVYRgI0%3D&langpref=en&role=customer";
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_dexServlet(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/CustomerIndexServlet?redirectpage=null&userLoginId=M9digAZ7pzeXHmPewnIdQAqGr21xmqXItVeXRhE4GeZSr4DeSCqVblvPYvDdWH8uLgSWUdjHMnYExJE8D1kO4plcXSlZu763yP2TQa0a2oECiIOEneVbfWUoFsmo9JWJ&url=D6sx6IOop3YBQDDexNmIHzNzfnmdLX6pXxUZZVYRgI0=");

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=FAA6DF141E97E05FEB264F0D6B709780-n3; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_iptServlet(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/JavaScriptServlet");

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Referer = "http://my.alpnames.com/servlet/CustomerIndexServlet?redirectpage=null&userLoginId=M9digAZ7pzeXHmPewnIdQAqGr21xmqXItVeXRhE4GeZSr4DeSCqVblvPYvDdWH8uLgSWUdjHMnYExJE8D1kO4plcXSlZu763yP2TQa0a2oECiIOEneVbfWUoFsmo9JWJ&url=D6sx6IOop3YBQDDexNmIHzNzfnmdLX6pXxUZZVYRgI0=";
                request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=FAA6DF141E97E05FEB264F0D6B709780-n3; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }
        #endregion

        private bool Request_securelogin_org(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://securelogin.org/login.php");
                request.CookieContainer = cookieContainer;
                AddIfProxyEnabled(request);


                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Referer = "http://alpnames.com/login.php";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=d51412c1740b610d61eba2a4e2da922f81454427083; customer_preferred_display_currency=INR; selected_lang=en; PHPSESSID=tr1870rup5anc6dh3b1o8r9842");
                request.KeepAlive = true;
                request.ContentType = "application/x-www-form-urlencoded";

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"action=secure_login&redirecturl=http%3A%2F%2Falpnames.com%2F&resellerid=516804&txtUserName=" +
                    HttpUtility.UrlEncode(username) + "&txtPassword=" + HttpUtility.UrlEncode(password) + "&submit=Login+now";
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception ex)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_alpnames_com(out HttpWebResponse response)
        {
            response = null;

            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(responseUri);
                request.CookieContainer = cookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Referer = "http://alpnames.com/login.php";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; customer_preferred_display_currency=INR; selected_lang=en; custemail=adytech2010%40gmail.com; custname=Pierre+Demarque; custid=14249633; PHPSESSID=fvq5iv25gctdsq87e2ao97bg17; pnctest=1; fc_g=%7B%22session_geo%22%3A%22%7B%5C%22locale%5C%22%3A%7B%5C%22country%5C%22%3A%5C%22us%5C%22%2C%5C%22lang%5C%22%3A%5C%22en%5C%22%7D%2C%5C%22current_session%5C%22%3A%7B%5C%22url%5C%22%3A%5C%22http%3A%2F%2Falpnames.com%2Flogin.php%5C%22%7D%2C%5C%22browser%5C%22%3A%7B%5C%22browser%5C%22%3A%5C%22Firefox%5C%22%2C%5C%22version%5C%22%3A43%2C%5C%22os%5C%22%3A%5C%22Windows%5C%22%7D%2C%5C%22device%5C%22%3A%7B%5C%22is_tablet%5C%22%3Afalse%2C%5C%22is_phone%5C%22%3Afalse%2C%5C%22is_mobile%5C%22%3Afalse%7D%7D%22%2C%22location%22%3A%22%7B%5C%22latitude%5C%22%3A%5C%2223.916700%5C%22%2C%5C%22longitude%5C%22%3A%5C%2287.533302%5C%22%2C%5C%22address%5C%22%3A%7B%5C%22city%5C%22%3A%5C%22Suri%5C%22%2C%5C%22region%5C%22%3A%5C%22West%20Bengal%5C%22%2C%5C%22country%5C%22%3A%5C%22India%5C%22%2C%5C%22country_code%5C%22%3A%5C%22IN%5C%22%7D%2C%5C%22source%5C%22%3A%5C%22google%5C%22%2C%5C%22ipAddress%5C%22%3A%5C%22106.219.29.225%5C%22%2C%5C%22version%5C%22%3A0.4%7D%22%7D; fc_vid=visitor33048704934; _gat=1; _dc_gtm_UA-53818056-1=1");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception ex)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_alpnames_com_1(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://alpnames.com/");
                request.CookieContainer = cookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Referer = responseUri;
                //"http://alpnames.com/login.php?action=login_complete&from_checkout=false&customer_id=14249633&hash=ae7503a2ce8815e272e116bbcca00c7ee92e52c0ab59b0119f56d6c89788634b";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; customer_preferred_display_currency=INR; selected_lang=en; custemail=adytech2010%40gmail.com; custname=Pierre+Demarque; custid=14249633; PHPSESSID=fvq5iv25gctdsq87e2ao97bg17; pnctest=1; fc_g=%7B%22session_geo%22%3A%22%7B%5C%22locale%5C%22%3A%7B%5C%22country%5C%22%3A%5C%22us%5C%22%2C%5C%22lang%5C%22%3A%5C%22en%5C%22%7D%2C%5C%22current_session%5C%22%3A%7B%5C%22url%5C%22%3A%5C%22http%3A%2F%2Falpnames.com%2Flogin.php%5C%22%7D%2C%5C%22browser%5C%22%3A%7B%5C%22browser%5C%22%3A%5C%22Firefox%5C%22%2C%5C%22version%5C%22%3A43%2C%5C%22os%5C%22%3A%5C%22Windows%5C%22%7D%2C%5C%22device%5C%22%3A%7B%5C%22is_tablet%5C%22%3Afalse%2C%5C%22is_phone%5C%22%3Afalse%2C%5C%22is_mobile%5C%22%3Afalse%7D%7D%22%2C%22location%22%3A%22%7B%5C%22latitude%5C%22%3A%5C%2223.916700%5C%22%2C%5C%22longitude%5C%22%3A%5C%2287.533302%5C%22%2C%5C%22address%5C%22%3A%7B%5C%22city%5C%22%3A%5C%22Suri%5C%22%2C%5C%22region%5C%22%3A%5C%22West%20Bengal%5C%22%2C%5C%22country%5C%22%3A%5C%22India%5C%22%2C%5C%22country_code%5C%22%3A%5C%22IN%5C%22%7D%2C%5C%22source%5C%22%3A%5C%22google%5C%22%2C%5C%22ipAddress%5C%22%3A%5C%22106.219.29.225%5C%22%2C%5C%22version%5C%22%3A0.4%7D%22%7D; fc_vid=visitor33048704934; _gat=1; _dc_gtm_UA-53818056-1=1; userloggedin=yes");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception ex)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_getCsrf(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/JavaScriptServlet");
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                //request.Referer = "http://my.alpnames.com/servlet/CustomerIndexServlet?redirectpage=null&userLoginId=qVPUlHpXTqvJDjzs73CCzH28xdAXYQKDjlypBLHHQKlrG1dorbZkgzLql2dFp1AemokVXHXIfY954NbSmdGd9g70Q9clhkme9sCiN5fZVBPpsTCBt2UbjnJrovGxuwWG&url=gPfj/azGA8yf4szIvWHA28fUDt9yI1WJwZz6BUHAeqM=";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; JSESSIONID=F2DAE83F82522250B91DB7DF4CA19FE7-n4; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true; _gat=1; _dc_gtm_UA-53818056-1=1");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_FetchOrderId(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/ListAllOrdersServlet?formaction=onlyOrdersList" +
                    "&recperpage=30&pageno=1&orderby=&forproduct=&creationTimeRangeEnd=&creationTimeRangeStart=&expiry=&currentstatus=&expiringInDays=30&searchstring=" +
                    searchString + "&searchfor=domain&show_only_expiring_orders=false&nameserver=&raastatus=&ppExpiringInDays=45&ppExpiry=any");
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add("X-NewRelic-ID", @"VQcEUVZUGwoEXVdaBw==");
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest, OWASP CSRFGuard Project");
                request.Headers.Add("OWASP_CSRFTOKEN", csrfToken);
                request.Referer = "http://my.alpnames.com/servlet/ListAllOrdersServlet?formaction=listOrders";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=E344C8B94CB1499CD6A5AF94ABB6F673-n1; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_getRefKey(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/ViewOrderServlet?orderid=" + orderid);
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Referer = "http://my.alpnames.com/servlet/ListAllOrdersServlet?formaction=listOrders";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; JSESSIONID=3EDB9517E56A6CA4D24AD2827B05CA69-n4; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_openUpdatenameserverWindow(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/ManageNsServlet?validatenow=false&orderid=" + orderid +
                    "&domainname=" + searchString + "&productcategory=domorder");
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add("X-NewRelic-ID", @"VQcEUVZUGwoEXVdaBw==");
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest, OWASP CSRFGuard Project");
                request.Headers.Add("OWASP_CSRFTOKEN", csrfToken);
                //request.Referer = "http://my.alpnames.com/servlet/ViewDomainServlet?orderid=65444366&referrerkey=TWhiZjBsTlhLZGhpK1B4b1M0VEQxZzMyTlFqblJ5VHJvd0JvL2VzcXVFdnljQnhRWEI1aERRPT0=";
                request.Referer = responseUri;
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; JSESSIONID=1F18089CA4FA6D4D90FDD4559B99615B-n2; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true; _gat=1");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_updateNameservers(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/ManageNsServlet");
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add("X-NewRelic-ID", @"VQcEUVZUGwoEXVdaBw==");
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest, OWASP CSRFGuard Project");
                request.Headers.Add("OWASP_CSRFTOKEN", csrfToken);
                request.Referer = responseUri;
                //"http://my.alpnames.com/servlet/ViewDomainServlet?orderid=65444366&referrerkey=WGxQV250YXdraDlncldEc1lZQTM5U2VLcmlHSWtEMGd0V0kyL1ZwS2NkUGZudmFSakdkaWRRPT0=";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=3EDB9517E56A6CA4D24AD2827B05CA69-n4; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = "orderid=" + orderid + "&ns1=" + ns1 + "&ns2=" + ns2 + "&ns3=" + ns3;
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_getContactId(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/ModifyDomainContactServlet?validatenow=false&orderid=" +
                    orderid + "&domainname=" + searchString);
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add("X-NewRelic-ID", @"VQcEUVZUGwoEXVdaBw==");
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest, OWASP CSRFGuard Project");
                request.Headers.Add("OWASP_CSRFTOKEN", csrfToken);
                request.Referer = responseUri;
                //"http://my.alpnames.com/servlet/ViewDomainServlet?orderid=65444366&referrerkey=UWs3WFU4RG04Vm1XTWhKUTg1MktYS05IMjdOa08zTHJ6Qk9PL1VOSVFxYVJ1bjl2Z2hnY3VBPT0=";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; JSESSIONID=177929D2D1350A01F62B9FDE316CA7C9-n4; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool Request_my_alpnames_com_updateAddress(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://my.alpnames.com/servlet/ModifyDomainContactServlet");
                request.CookieContainer = myAlpCookieContainer;
                AddIfProxyEnabled(request);

                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add("X-NewRelic-ID", @"VQcEUVZUGwoEXVdaBw==");
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest, OWASP CSRFGuard Project");
                request.Headers.Add("OWASP_CSRFTOKEN", csrfToken);
                request.Referer = responseUri;
                //"http://my.alpnames.com/servlet/ViewDomainServlet?orderid=65444366&referrerkey=WGxQV250YXdraDlncldEc1lZQTM5U2VLcmlHSWtEMGd0V0kyL1ZwS2NkUGZudmFSakdkaWRRPT0=";
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=de31141f3efcce0ee1ba692b2599af2e71454427039; _ga=GA1.2.1789643051.1454427052; _gat=1; _dc_gtm_UA-53818056-1=1; JSESSIONID=3EDB9517E56A6CA4D24AD2827B05CA69-n4; CURRENT_URL=http%3A%2F%2Fmy.alpnames.com; role=customer; parentid=516804; logged=true");
                request.KeepAlive = true;

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string name = GetRandomValue(RandomValuePicker.Name);
                string address = GetRandomValue(RandomValuePicker.Address);
                string organisation = GetRandomValue(RandomValuePicker.Organisation);
                string email = GetRandomValue(RandomValuePicker.Email);
                string city = GetRandomValue(RandomValuePicker.City);
                string zip = GetRandomValue(RandomValuePicker.ZipCode);
                string phones = GetRandomValue(RandomValuePicker.Phone);
                string region = GetRandomValue(RandomValuePicker.Region);
                string country = GetRandomValue(RandomValuePicker.Country);
                if (!string.IsNullOrWhiteSpace(country))
                {
                    string[] splits = country.Split(',');
                    if (splits != null && splits.Count() > 0)
                    {
                        country = splits[0].Replace("\"", string.Empty);
                    }
                }
                string[] splitPhones = phones.Split(',');
                string phone1 = string.Empty;
                string phone2 = string.Empty;
                if (splitPhones.Length > 0)
                {
                    phone1 = splitPhones[0];
                    if (splitPhones.Length > 1)
                        phone2 = splitPhones[1];
                }
                string[] domainTypeSplits = searchString.Split('.');
                string domainType = string.Empty;
                if (domainTypeSplits != null && domainTypeSplits.Count() > 0)
                {
                    domainType = "dot" + domainTypeSplits[domainTypeSplits.Count() - 1];
                }
                string body = "name=" + name.Replace(' ', '+') + "&contactid=" +
                    contactid + "&oldname=" + name.Replace(' ', '+') + "&domaintldtype=" + domainType +
                    "&contacttype=registrant&type=Contact&address=" + address.Replace(' ', '+') + "&company=" +
                    organisation.Replace(' ', '+') + "&oldcompany=" + organisation.Replace(' ', '+') + "&emailaddr=" +
                    HttpUtility.UrlEncode(email) + "&city=" + city + "&zip=" + zip + "&telnocc=" + phone1 + "&telno=" + phone2 +
                    "&state=" + region + "&not_applicable=false&faxnocc=&faxno=&country=" + country + "&samecontacts=true&orderid=" + orderid + "&registrantcontactid=" + contactid
                    + "&admincontactid=" + adminContactid + "&billingcontactid=" + billingContactid + "&techcontactid=" +
                    techContactid + "&domainname=" + searchString;
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private string GetRandomValue(RandomValuePicker name)
        {
            string result = string.Empty;
            string fileName = string.Empty;

            try
            {
                if (name == RandomValuePicker.Name)
                    fileName = "name.txt";
                else if (name == RandomValuePicker.Address)
                    fileName = "address.txt";
                else if (name == RandomValuePicker.Organisation)
                    fileName = "organisation.txt";
                else if (name == RandomValuePicker.Email)
                    fileName = "email.txt";
                else if (name == RandomValuePicker.City)
                    fileName = "city.txt";
                else if (name == RandomValuePicker.ZipCode)
                    fileName = "zip.txt";
                else if (name == RandomValuePicker.Region)
                    fileName = "region.txt";
                else if (name == RandomValuePicker.Phone)
                    fileName = "phone.txt";
                else if (name == RandomValuePicker.Country)
                    fileName = "country.txt";

                string[] lines = File.ReadAllLines(fileName);
                int count = lines.Count();
                Random rnd = new Random();
                int index = rnd.Next(0, count - 1);
                result = lines[index];
                if (name == RandomValuePicker.Address)
                {
                    int val = rnd.Next(1, 1000);
                    result = val + " " + result;
                }

            }
            catch (Exception)
            {

            }


            return result;
        }
    }
}
