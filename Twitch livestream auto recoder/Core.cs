using System;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Twitch_livestream_auto_recoder;
using System.Windows.Forms;

namespace RecoderCore
{

    public enum Status { online = 0, NotFound, error, Not_determined }


    //It is a sotrage this program stting to static value
    [Serializable]
    class Settings
    {
        //This vlaue is to storege how many refres to chek 
        //if the tiwich stream online
        public static int Refresh = 30;

        public static string UserName = "";
        public static string Quality = "";
        public static string Directory = @"";

        //Input your Client id from witch deloper page
        public static string ClientID = "";

        public static string version = "1.0.2";
        public static bool TurnOffAfterRecod = false;
    }


    class StreamMonitor
    {
        LogWritedelegate LogWrite;
        StatusBoxUpdateDelegate StatusBoxUpdate;
        Button3EnabledSetDelegate button3EnabledSet;

        public StreamMonitor(LogWritedelegate logWritedelegate, StatusBoxUpdateDelegate statusBoxUpdate, Button3EnabledSetDelegate button3EnabledSetD)
        {
            LogWrite = logWritedelegate;

            StatusBoxUpdate = statusBoxUpdate;

            button3EnabledSet = button3EnabledSetD;
        }

        private Status ChekUser()
        {

            Status status = Status.Not_determined;
            ///<summary>
            ///json result
            /// </summary>
            JObject info = null;

            try
            {
                string ApiURL;
                HttpWebRequest request;
                ApiURL = "https://api.twitch.tv/kraken/streams/" + Settings.UserName;
                request = (HttpWebRequest)WebRequest.Create(ApiURL);
                request.Headers.Add("client-id: " + Settings.ClientID);

                Stream ResponseStream;
                HttpWebResponse response;

                string result = null;
                response = (HttpWebResponse)request.GetResponse();
                ResponseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(ResponseStream);
                result = reader.ReadToEnd();
                info = JObject.Parse(result);
                ResponseStream.Close();
                response.Close();

                if (info["stream"].ToString() == "")
                {
                    status = Status.NotFound;
                }
                else
                {
                    status = Status.online;
                }

            }
            catch (Exception e)
            {

                LogWrite("ERROR:" + e.Message);
                status = Status.error;
            }

            return status;
        }

        public void LoopChack()
        {
            while (true)
            {

                Status UserStatus = ChekUser();

                StatusBoxUpdate(UserStatus);

                if (UserStatus == Status.NotFound)
                {
                    LogWrite("현재 스트림을 찾을 수 없습니다. 스트리머 ID:" + Settings.UserName);

                }
                else if (UserStatus == Status.online)
                {
                    LogWrite("스트림이 현재 온라인 상태입니다.");
                    StartRecord();

                    StartChekProcess();
                }

                while (Form1.ProcessIsPause == true)
                {
                    Thread.Sleep(500);
                }

                LogWrite(Settings.Refresh.ToString() + " 초 후에 다시 확인합니다.");
                Thread.Sleep(Settings.Refresh * 1000);

            }

        }

        private void StartChekProcess()
        {
            while (true)
            {

                while (Form1.ProcessIsPause == true)
                {
                    Thread.Sleep(500);
                }

                Process[] ProcessList = Process.GetProcessesByName("livestreamer");

                if (ProcessList.Length > 1)
                {
                    LogWrite("주의: 두개 이상의 livestreamer 프로세스가 실행 중 입니다. ");
                }
                else if (ProcessList.Length == 0)
                {
                    Status status = ChekUser();

                    if (status == Status.online)
                    {
                        LogWrite("경고: Livestreamer가 알 수 없는 이유로 종료되었습니다.");
                        for (int i = 5; i > 0; i--)
                        {
                            LogWrite(i.ToString() + " 초 후에 재시작...");
                            Thread.Sleep(1000);
                        }
                        StartRecord();

                    }
                    else if (status == Status.NotFound)
                    {
                        LogWrite("스트림이 끝났습니다.");

                        if (Settings.TurnOffAfterRecod == true)
                        {
                            LogWrite("컴퓨터를 종료합니다.");

                            MessageBox.Show("스트림이 끝나서 컴퓨터를 종료합니다.\n" +
                             "컴퓨터가 종료되는 것을 원치 않으시면 30초 이내에 cmd를 실행하고 \nshutdown -a를 " +
                            "입력하시거나 종료 취소 버튼을 눌러주세요");

                            Process.Start("shutdown.exe", "-s -t 45");

                            button3EnabledSet(true);
                        }
                        break;
                    }

                }

                Thread.Sleep(100);

            }

        }

        private void StartRecord()
        {
            string FileName = Settings.UserName + "-" + DateTime.Now.Year + "Y-" + DateTime.Now.Month + "M-"
                                  + DateTime.Now.Hour + "H-" + DateTime.Now.Minute + "M";
            LogWrite("파일 이름: " + FileName);

            string argmuments = "--http-header Client-ID=" + Settings.ClientID + " -o " + '"' + Settings.Directory + "/" +
                    FileName + ".mp4" + '"' + " twitch.tv/" + Settings.UserName + " " + Settings.Quality;


            LogWrite("시스템 호출:" + "livestreamer " + argmuments);

            Process.Start("livestreamer.exe", argmuments);
        }

    }
}

