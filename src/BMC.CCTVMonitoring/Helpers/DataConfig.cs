using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BMC.CCTVMonitoring.Helpers
{
    public class DataConfig
    {
        const string ConfigFile = "Config.json";
        public static string[] RoomName = { "backyard", "parking area", "living room", "top floor" };
        public string MqttHost { set; get; } = "broker.emqx.io";//"13.76.156.239";
        public string MqttUser { set; get; } = "loradev_mqtt";
        public string MqttPass { set; get; } = "test123";
        public string MailTo { set; get; } = "mifmasterz@gmail.com";
        public string MailFrom { set; get; } = "mifmasterz@outlook.com";
        public string SmsTo { set; get; } = "+628174810345";

        public string UrlMail { set; get; } = "https://bmcsecurityweb.azurewebsites.net/svc/sendmail.ashx";
        public string UrlSms { set; get; } = "https://bmcsecurityweb.azurewebsites.net/svc/sms.ashx";
        public int EvalInterval { set; get; } = 3000;
        public int CCTVCount { set; get; } = 4;
        public string CCTV_IP  { set; get; }= "192.168.1.10";
        public int CaptureIntervalSecs { set; get; } = 30;
        //,  
        public List<string> CCTVURL = new List<string> { $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=0&u=admin&p=&q=0&d=1&rand=",
        $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=1&u=admin&p=&q=0&d=1&rand=",
        $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=2&u=admin&p=&q=0&d=1&rand=",
        $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=3&u=admin&p=&q=0&d=1&rand="};
        public DataConfig()
        {
            if (!IsConfigExist())
                Save();
            else
                Load();
            for(var i=0;i<CCTVURL.Count;i++) {
                CCTVURL[i] = CCTVURL[i].Replace("[CCTV_IP]", this.CCTV_IP);
            }
        }

        bool IsConfigExist()
        {
            return File.Exists(ConfigFile);
        }
        public void Save()
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(ConfigFile, json);
        }

        void Load()
        {
            var json = File.ReadAllText(ConfigFile);
            var obj = JsonConvert.DeserializeObject<DataConfig>(json);
            if (obj == null) return;
            this.SmsTo = obj.SmsTo;
            this.UrlMail = obj.UrlMail;
            this.UrlSms = obj.UrlSms;
            this.CCTVURL = obj.CCTVURL;
            this.CCTVCount = obj.CCTVCount;
            this.EvalInterval = obj.EvalInterval;
            this.CCTV_IP = obj.CCTV_IP;
            this.CaptureIntervalSecs = obj.CaptureIntervalSecs;
            this.MailFrom = obj.MailFrom;
            this.MailTo = obj.MailTo;
            this.MqttHost = obj.MqttHost;
            this.MqttPass = obj.MqttPass;
            this.MqttUser = obj.MqttUser;
            
        }
    }
}
