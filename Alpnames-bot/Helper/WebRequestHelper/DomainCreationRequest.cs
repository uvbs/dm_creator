using Alpnames_bot.Helper.JavascriptHelper;
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alpnames_bot.Helper.WebRequestHelper
{
    class DomainCreationRequest
    {
        private CookieContainer cookieContainer = new CookieContainer();
        //private CookieContainer myAlpCookieContainer = new CookieContainer();

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
        private string apiToken;
        private string email;
        private long emailId;
        private string linkToConfirm;

        private string proxy { get; set; }
        private string sessionId { get; set; }
        private string token { get; set; }



        public DomainCreationRequest(DataTable dtRecords, int index, string domain, string dns1, string dns2, CancellationToken cancellationToken)
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

        public void MakeRequests()
        {
            try
            {
                HttpWebResponse response;
                string responseText = string.Empty;

                CheckIfCancellationRequested();

                // email creation starts
                if (Request_www_guerrillamail_com(out response))
                {
                    responseText = ReadResponse(response);
                    sessionId = response.GetResponseHeader("set-cookie");
                    if (!string.IsNullOrWhiteSpace(sessionId))
                    {
                        lock (dtRecords)
                        {
                            dtRecords.Rows[index]["sessionId"] = sessionId;
                        }
                    }
                    response.Close();
                }

                email = JsonOperator.GetJsonValueForKey(responseText, StringHelper.Constants.EmailKey);
                apiToken = JsonOperator.GetJsonValueForKey(responseText, StringHelper.Constants.ApiKey);

                CheckIfCancellationRequested();

                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = string.Format("Email...{0}{1}", email, StringHelper.Constants.EmailDomain);
                }
                // email creation ends

                // checking for domain availability

                CheckIfCancellationRequested();

                if (Request_my_freenom_com(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();

                if (Request_my_freenom_com_isAvailable(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }


                JsonDomainObject jsonDomainObject = JsonOperator.GetJsonDomainObject(responseText);

                if (jsonDomainObject != null &&
                    jsonDomainObject.free_domains != null &&
                    jsonDomainObject.free_domains.Count > 0 &&
                    !jsonDomainObject.free_domains[0].status.Equals("available", StringComparison.OrdinalIgnoreCase))
                {
                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["status"] = "Domain not available";
                    }
                }
                else
                {
                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["status"] = "Domain available";
                    }
                }

                CheckIfCancellationRequested();

                // checking for domain availability ends



                // adding to basket 
                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "adding to basket...";
                }

                if (Request_my_freenom_com_add_basket_2(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();


                if (Request_my_freenom_com_cart_php(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();


                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(responseText);

                var tokenNodes = doc.DocumentNode.SelectNodes("//input[@type='hidden']"); ;
                token = string.Empty;
                if (tokenNodes != null && tokenNodes.Count() > 2)
                {
                    // 3rd element has the value for token
                    var tokenNode = tokenNodes.ToArray()[2];
                    token = tokenNode.Attributes["value"].Value;
                }

                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "adding to basket ends...";
                }
                // adding to basket ends

                // getting price list 
                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "getting price list...";
                }

                if (Request_my_freenom_com_ricing_php(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();


                if (Request_my_freenom_com_update_php(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();


                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "getting price list ends...";
                }
                // getting price list

                // booking domain
                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "booking domain starts...";
                }
                if (Request_my_freenom_com_figure_php(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();


                if (Request_my_freenom_com_cart_php_1(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();


                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "booking...email to be sent...";
                }


                if (Request_my_freenom_com_send_email(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }

                CheckIfCancellationRequested();
                if (responseText.ToLower().Contains("A user already exists".ToLower()))
                {
                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["status"] = "A user already exists";
                        return;
                    }
                }
                else if (responseText.ToLower().Contains("not valid".ToLower()))
                {
                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["status"] = "Email entered is not valid";
                        return;
                    }
                }
                else
                {
                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["status"] = "email sent...waiting for 3 seconds";
                    }
                }

                // booking domain ends


                // checking for new mail
                emailId = -1;
                while (emailId <= 0)
                {
                    Thread.Sleep(3000); // waiting for 3 seconds
                    if (Request_www_guerrillamail_com_checking_for_mail(out response))
                    {
                        responseText = ReadResponse(response);

                        response.Close();
                    }
                    CheckIfCancellationRequested();
                    emailId = JsonOperator.GetEmailIdFromEmailsRead(responseText);
                    if (emailId <= 0)
                    {
                        lock (dtRecords)
                        {

                            dtRecords.Rows[index]["status"] = "Retrying...mail from freenom not received";
                        }

                    }
                }

                lock (dtRecords)
                {

                    dtRecords.Rows[index]["status"] = "mail from freenom received";
                }
                // checking mail ends      

                // opening mail from freenom
                if (Request_www_guerrillamail_com_opening_mail_from_freenom(out response))
                {
                    responseText = ReadResponse(response);

                    response.Close();
                }
                CheckIfCancellationRequested();

                // parsing HTML
                
                doc = new HtmlDocument();
                doc.LoadHtml(responseText);
                var spanNode = doc.DocumentNode.SelectSingleNode("//span");
                if (spanNode != null)
                {
                    var childNodes = spanNode.ChildNodes;
                    if(childNodes != null && childNodes.Count > 0)
                    {
                        var aNode = childNodes[0];
                        linkToConfirm = aNode.Attributes["href"]?.Value;
                    }
                }

                if(!string.IsNullOrWhiteSpace(linkToConfirm))
                {
                    if (Request_openingLink_in_email(out response))
                    {
                        responseText = ReadResponse(response);

                        response.Close();
                    }
                    CheckIfCancellationRequested();

                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["status"] = "Booking confirmed...";
                    }
                }
                else
                {
                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["status"] = "Link not received in email...";
                    }
                }

                // opening mail from freenom ends

            }
            catch (OperationCanceledException)
            {
                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "Cancelled...";

                }
            }
            catch (Exception)
            {
                lock (dtRecords)
                {

                    dtRecords.Rows[index]["status"] = "Error occured...";
                }
            }

        }

        private bool Request_openingLink_in_email(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(linkToConfirm);
                request.KeepAlive = true;
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");

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

        private bool Request_www_guerrillamail_com_opening_mail_from_freenom(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request =
                    (HttpWebRequest)WebRequest.Create("https://www.guerrillamail.com/ajax.php?f=fetch_email&email_id=mr_" + emailId.ToString() + "&site=guerrillamail.com");

                request.KeepAlive = true;
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.Headers.Set(HttpRequestHeader.Authorization, "ApiToken " + apiToken);
                request.Referer = "https://www.guerrillamail.com/";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                request.Headers.Set(HttpRequestHeader.Cookie, dtRecords.Rows[index]["sessionId"].ToString());

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

        private bool Request_my_freenom_com_send_email(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/cart.php?a=checkout");
                request.CookieContainer = cookieContainer;

                request.KeepAlive = true;
                request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
                request.Headers.Add("Origin", @"https://my.freenom.com");
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request.Referer = "https://my.freenom.com/cart.php?a=checkout";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"WHMCSZH5eHTGhfvzP=35uh4t8kmedr183hjkqq41drn1; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.1176718596.1501523537; _gid=GA1.2.1340478577.1501523537; __utmt=1; __utma=76711234.1176718596.1501523537.1501523583.1501523583.1; __utmb=76711234.5.10.1501523583; __utmc=76711234; __utmz=76711234.1501523583.1.1.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html; fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""4lpy8Ri8mKwtFx+ylE4/ZsxOKJBzfjgX56j3cBtzd1M=""");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"token=" + token + "&verifyemail=true&myemail=" + email + "@pokemail.net";
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

        private bool Request_www_guerrillamail_com_checking_for_mail(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.guerrillamail.com/ajax.php?f=check_email&seq=1&site=guerrillamail.com");


                request.KeepAlive = true;
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.Headers.Set(HttpRequestHeader.Authorization, "ApiToken " + apiToken);
                request.Referer = "https://www.guerrillamail.com/";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                request.Headers.Set(HttpRequestHeader.Cookie, dtRecords.Rows[index]["sessionId"].ToString());

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

        private bool Request_my_freenom_com_cart_php_1(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/cart.php?a=confdomains");
                request.CookieContainer = cookieContainer;

                request.KeepAlive = true;
                request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
                request.Headers.Add("Origin", @"https://my.freenom.com");
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request.Referer = "https://my.freenom.com/cart.php?a=confdomains&language=english";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""dpydHC9pmmIJCueEh0JF7bUvM5EyfS4vg88ZcpUpBww=""; WHMCSZH5eHTGhfvzP=0stqsilf896kfg2mg2octqhf67; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.1062950231.1500606029; _gid=GA1.2.1752626094.1501170856; __utma=76711234.1062950231.1500606029.1500622941.1501257898.4; __utmb=76711234.1.10.1501257898; __utmc=76711234; __utmz=76711234.1501257898.4.2.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"token=" + token + "&update=true&" + domain +
                    "_tk_period=12M&idprotection%5B0%5D=on&domainns1=ns01.freenom.com&domainns2=ns02.freenom.com&domainns3=ns03.freenom.com&domainns4=ns04.freenom.com&domainns5=";
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

        private bool Request_my_freenom_com_figure_php(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/includes/domains/domainconfigure.php");
                request.CookieContainer = cookieContainer;

                request.KeepAlive = true;
                request.Accept = "*/*";
                request.Headers.Add("Origin", @"https://my.freenom.com");
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "https://my.freenom.com/cart.php?a=confdomains&language=english";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""dpydHC9pmmIJCueEh0JF7bUvM5EyfS4vg88ZcpUpBww=""; WHMCSZH5eHTGhfvzP=0stqsilf896kfg2mg2octqhf67; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.1062950231.1500606029; _gid=GA1.2.1752626094.1501170856; __utma=76711234.1062950231.1500606029.1500622941.1501257898.4; __utmb=76711234.1.10.1501257898; __utmc=76711234; __utmz=76711234.1501257898.4.2.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"data=%7B%22" + domain + ".tk%22%3A%7B%22hn1%22%3A%22" + domain +
                    ".tk%22%2C%22hi1%22%3A%22" + dns1 + "%22%2C%22hn2%22%3A%22www." + domain + ".tk%22%2C%22hi2%22%3A%22" + dns2 + "%22%7D%7D";
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

        private bool Request_my_freenom_com_update_php(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/includes/domains/confdomain-update.php");
                request.CookieContainer = cookieContainer;

                request.KeepAlive = true;
                request.Accept = "*/*";
                request.Headers.Add("Origin", @"https://my.freenom.com");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "https://my.freenom.com/cart.php?a=confdomains&language=english";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""dpydHC9pmmIJCueEh0JF7bUvM5EyfS4vg88ZcpUpBww=""; WHMCSZH5eHTGhfvzP=0stqsilf896kfg2mg2octqhf67; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.1062950231.1500606029; _gid=GA1.2.1752626094.1501170856; __utma=76711234.1062950231.1500606029.1500622941.1501257898.4; __utmb=76711234.1.10.1501257898; __utmc=76711234; __utmz=76711234.1501257898.4.2.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"domain=" + domain + ".tk&period=12M";
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

        private bool Request_my_freenom_com_ricing_php(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/includes/domains/confdomain-pricing.php");
                request.CookieContainer = cookieContainer;
                request.KeepAlive = true;
                request.Accept = "*/*";
                request.Headers.Add("Origin", @"https://my.freenom.com");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "https://my.freenom.com/cart.php?a=confdomains&language=english";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__utma=76711234.1062950231.1500606029.1500614011.1500622941.3; __utmz=76711234.1500606179.1.1.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html; fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""dpydHC9pmmIJCueEh0JF7bUvM5EyfS4vg88ZcpUpBww=""; WHMCSZH5eHTGhfvzP=0stqsilf896kfg2mg2octqhf67; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.1062950231.1500606029; _gid=GA1.2.1752626094.1501170856");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"domains%5B%5D=" + domain + ".tk";
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

        private bool Request_my_freenom_com_cart_php(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/cart.php?a=confdomains&language=english");
                request.CookieContainer = cookieContainer;

                request.KeepAlive = true;
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request.Referer = "http://www.freenom.com/en/index.html";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__utma=76711234.1062950231.1500606029.1500614011.1500622941.3; __utmz=76711234.1500606179.1.1.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html; fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""dpydHC9pmmIJCueEh0JF7bUvM5EyfS4vg88ZcpUpBww=""; WHMCSZH5eHTGhfvzP=0stqsilf896kfg2mg2octqhf67; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.1062950231.1500606029; _gid=GA1.2.1752626094.1501170856");

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

        private bool Request_my_freenom_com_add_basket_2(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/includes/domains/fn-additional.php");
                request.CookieContainer = cookieContainer;
                request.KeepAlive = true;
                request.Accept = "*/*";
                request.Headers.Add("Origin", @"http://www.freenom.com");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "http://www.freenom.com/en/index.html";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"__utma=76711234.1062950231.1500606029.1500614011.1500622941.3; __utmz=76711234.1500606179.1.1.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html; fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""dpydHC9pmmIJCueEh0JF7bUvM5EyfS4vg88ZcpUpBww=""; WHMCSZH5eHTGhfvzP=0stqsilf896kfg2mg2octqhf67; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.1062950231.1500606029; _gid=GA1.2.1752626094.1501170856");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"domain=" + domain + "&tld=.tk";
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

        private bool Request_my_freenom_com_isAvailable(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/includes/domains/fn-available.php");
                request.CookieContainer = cookieContainer;

                request.KeepAlive = true;
                request.Accept = "*/*";
                request.Headers.Add("Origin", @"http://www.freenom.com");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "http://www.freenom.com/en/index.html";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"WHMCSZH5eHTGhfvzP=0stqsilf896kfg2mg2octqhf67; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; fp_token_7c6a6574-f011-4c9a-abdd-9894a102ccef=""dpydHC9pmmIJCueEh0JF7bUvM5EyfS4vg88ZcpUpBww=""; __utmt=1; __utma=76711234.1062950231.1500606029.1501257898.1501264331.5; __utmb=76711234.1.10.1501264331; __utmc=76711234; __utmz=76711234.1501257898.4.2.utmcsr=freenom.com|utmccn=(referral)|utmcmd=referral|utmcct=/en/index.html; _ga=GA1.2.1062950231.1500606029; _gid=GA1.2.1752626094.1501170856; _gat=1");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"domain=" + domain + "&tld=";
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

        private bool Request_my_freenom_com(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://my.freenom.com/includes/domains/fn-additional.php");
                request.CookieContainer = cookieContainer;
                request.KeepAlive = true;
                request.Accept = "*/*";
                request.Headers.Add("Origin", @"http://www.freenom.com");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "http://www.freenom.com/en/index.html";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
                //request.Headers.Set(HttpRequestHeader.Cookie, @"WHMCSZH5eHTGhfvzP=nrhlt8tm041jm15138qufe52o5; AWSELB=BB755F330E44FE27E970EAECFCC78F629EB1F82E68A2EB4800BB8C05440CD44F87164DBFE8ADFF3E70BD458086728EC2CBAF4FA010B644897794A9E75D3F58371A29D2A8A2; _ga=GA1.2.125987736.1501173812; _gid=GA1.2.613361750.1501173812");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                string body = @"domain=" + domain + "&tld=.tk";
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

        private bool Request_www_guerrillamail_com(out HttpWebResponse response)
        {
            //checking for sessionid for blocks of 9
            int rem = index % 9;
            int quotient = index == 0 ? 0 : index / 9;
            //sessionId = string.Empty;
            for (int i = 9 * quotient; i < dtRecords.Rows.Count && i < 9 * (quotient + 1); i++)
            {
                if (i == index)
                    continue;
                if (!string.Empty.Equals(Convert.ToString(dtRecords.Rows[i]["sessionId"])))
                {
                    sessionId = Convert.ToString(dtRecords.Rows[i]["sessionId"]);
                    lock (dtRecords)
                    {
                        dtRecords.Rows[index]["sessionId"] = sessionId;
                    }
                    break;
                }
            }

            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.guerrillamail.com/");

                request.KeepAlive = true;
                request.Headers.Add("Upgrade-Insecure-Requests", @"1");
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");

                request.Headers.Set(HttpRequestHeader.Cookie, sessionId);

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

    }
}
