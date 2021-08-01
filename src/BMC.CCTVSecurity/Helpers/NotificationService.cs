using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BMC.CCTVSecurity.Helpers
{
    public class NotificationService
    {
        static HttpClient client;
        public static async Task<bool> SendMail(string subject,string message,string mailto,string mailfrom)
        {
            if (client == null) client = new HttpClient();
            var obj = new MailInfo() { body = message, from = mailfrom, subject= subject, mailto = mailto };
            var json = JsonConvert.SerializeObject(obj);
            var res = await client.PostAsync(DataConfig.UrlMail, new StringContent(json, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }

        public static async Task<bool> SendSms(string phoneto, string message)
        {
            if (client == null) client = new HttpClient();
            var obj = new SmsInfo() { message = message, phoneto = phoneto };
            var json = JsonConvert.SerializeObject(obj);
            var res = await client.PostAsync(DataConfig.UrlSms, new StringContent(json, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }
        public class MailInfo
        {
            public string mailto { get; set; }
            public string subject { get; set; }
            public string from { get; set; }
            public string body { get; set; }

        }
        public class SmsInfo
        {
            public string phoneto { get; set; }
            public string message { get; set; }

        }
    }
}
