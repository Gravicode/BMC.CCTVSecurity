using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMC.CCTVSecurity.Helpers
{
    public static class DataConfig
    {
        public const string MqttHost = "13.76.156.239";
        public const string MqttUser = "loradev_mqtt";
        public const string MqttPass = "test123";
        public const int CCTVCount = 4;
        const string CCTV_IP = "192.168.1.20";
        //,  
        public static string[] CCTVURL = new string[] {"https://garudainformasi.com/wp-content/uploads/2018/12/Dia-Memetik-Manfaat-Seumur-Hidup-4.jpg", "http://www.allwhitebackground.com/images/3/3816.jpg", "https://www.roomandboard.com/features/main/default/HH_OutFF_C0219/HH_OutFF_C0219.jpg", "http://walledoffhotel.com/img/image1.jpg" }; /*new string[] { $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=0&u=admin&p=&q=0&d=1&rand=",
        $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=1&u=admin&p=&q=0&d=1&rand=",
        $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=2&u=admin&p=&q=0&d=1&rand=",
        $"http://{CCTV_IP}/cgi-bin/snapshot.cgi?chn=3&u=admin&p=&q=0&d=1&rand="};*/
        static DataConfig() {
            

        }
    }
}
