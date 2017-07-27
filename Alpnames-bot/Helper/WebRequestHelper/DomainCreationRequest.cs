using Alpnames_bot.Helper.JavascriptHelper;
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

        private string proxy { get; set; }
        private string sessionId { get; set; }


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

                string email = JsonOperator.GetJsonValueForKey(responseText, StringHelper.Constants.EmailKey);

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

                JsonDomainObject jsonDomainObject = JsonOperator.GetJsonDomainObject(responseText);

                if (!jsonDomainObject.free_domains[0].status.Equals("available", StringComparison.OrdinalIgnoreCase))
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

                //TODO: Add to basket to be implemented
                
                // adding to basket ends



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
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
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
