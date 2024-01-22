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
        public string MqttHost { set; get; } 
        public string MqttUser { set; get; } 
        public string MqttPass { set; get; } 
        public string MailTo { set; get; } 
        public string MailFrom { set; get; } 
        public string SmsTo { set; get; }
        public string SnapshotDir { set; get; }
        public string UrlMail { set; get; } 
        public string UrlSms { set; get; } 
        public int EvalInterval { set; get; } 
        public int CCTVCount { set; get; } 
        public string CCTV_IP  { set; get; }
        public int CaptureIntervalSecs { set; get; }
        //IDCH
        public string StorageEndpoint { set; get; }
        public string StorageAccess { set; get; }
        public string StorageSecret { set; get; }
        public string StorageBucket { set; get; }
        public bool AutoRun { get; set; }
        public bool SaveToDisk ;//{ get; set; }
        public bool PushToCloud ;//{ get; set; }
        public bool PlaySound ;//{ get; set; }
        public bool PatrolMode ;//{ get; set; }
        public List<string> CCTVURL { set; get; }
        public DataConfig()
        {
           
        }

        public void SetDefault()
        {
            MqttHost = "broker.emqx.io";//"13.76.156.239";
            MqttUser = "loradev_mqtt";
            MqttPass = "test123";
            MailTo = "mifmasterz@gmail.com";
            MailFrom = "mifmasterz@outlook.com";
            SmsTo = "+628174810345";

            UrlMail = "https://bmcsecurityweb.azurewebsites.net/svc/sendmail.ashx";
            UrlSms = "https://bmcsecurityweb.azurewebsites.net/svc/sms.ashx";
            EvalInterval = 3000;
            CCTVCount = 4;
            CCTV_IP = "192.168.1.10";
            CaptureIntervalSecs = 5;
            SnapshotDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "cctv");
            //,  
            CCTVURL = new List<string> { $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=0&u=admin&p=&q=0&d=1&rand=",
        $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=1&u=admin&p=&q=0&d=1&rand=",
        $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=2&u=admin&p=&q=0&d=1&rand=",
        $"http://[CCTV_IP]/cgi-bin/snapshot.cgi?chn=3&u=admin&p=&q=0&d=1&rand="};

            StorageEndpoint = "https://is3.cloudhost.id";
            StorageAccess = "30S2OUDU3IY39A6PQJ8L";
            StorageSecret = "80afWLvL8GOFG0qUUoPn3x9jdAuOZNtUnCruwgUL";
            StorageBucket = "cctv";
            AutoRun = true;

            PlaySound = true;
            PushToCloud = true;
            PatrolMode = true;
        }

        public void Init()
        {
            if (!IsConfigExist())
            {
                //set default setting
                this.SetDefault();
                Save();
            }
            else
                Load();
            for (var i = 0; i < CCTVURL.Count; i++)
            {
                CCTVURL[i] = CCTVURL[i].Replace("[CCTV_IP]", this.CCTV_IP);
            }
        }

        bool IsConfigExist()
        {
            var exist = File.Exists(ConfigFile);
            return exist;
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
            this.SnapshotDir = obj.SnapshotDir;
            this.AutoRun = obj.AutoRun;

            this.PatrolMode = obj.PatrolMode;
            this.PlaySound = obj.PlaySound;
            this.PushToCloud = obj.PushToCloud;
            this.SaveToDisk = obj.SaveToDisk;

            this.StorageAccess = obj.StorageAccess;
            this.StorageBucket = obj.StorageBucket;
            this.StorageEndpoint = obj.StorageEndpoint;
            this.StorageSecret = obj.StorageSecret;
            
        }
    }
}
