using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BMC.CCTVMonitoring.Helpers
{
    public class Sound
    {
        public static async Task PlaySound(string fname)
        {
            if (File.Exists(fname))
            {
                using (var audioFile = new AudioFileReader(fname))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(1000);
                    }
                }

            }
        }
    }
}
