﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMC.CCTVSecurity.Helpers
{
    public static class DataConfig
    {
        public static string[] RoomName = { "backyard","parking area","living room","top floor" };
        public const string MqttHost = "broker.emqx.io";//"13.76.156.239";
        public const string MqttUser = "loradev_mqtt";
        public const string MqttPass = "test123";
        public const string MailTo = "mifmasterz@gmail.com";
        public const string MailFrom = "mifmasterz@outlook.com";
        public const string SmsTo = "+628174810345";
    
        public const string UrlMail = "https://bmcsecurityweb.azurewebsites.net/svc/sendmail.ashx";
        public const string UrlSms = "https://bmcsecurityweb.azurewebsites.net/svc/sms.ashx";
        public const int EvalInterval = 3000;
        public const int CCTVCount = 4;
        const string CCTV_IP = "192.168.1.10";
        public const int CaptureIntervalSecs = 30;
        //,  
        public static string[] CCTVURL = new string[] { $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=0&u=admin&p=&q=0&d=1&rand=",
        $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=1&u=admin&p=&q=0&d=1&rand=",
        $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=2&u=admin&p=&q=0&d=1&rand=",
        $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=3&u=admin&p=&q=0&d=1&rand="};
        //new string[] {"https://garudainformasi.com/wp-content/uploads/2018/12/Dia-Memetik-Manfaat-Seumur-Hidup-4.jpg", "http://www.allwhitebackground.com/images/3/3816.jpg", "https://www.roomandboard.com/features/main/default/HH_OutFF_C0219/HH_OutFF_C0219.jpg", "http://walledoffhotel.com/img/image1.jpg" }; /**/
        static DataConfig() {
            

        }
    }
}
