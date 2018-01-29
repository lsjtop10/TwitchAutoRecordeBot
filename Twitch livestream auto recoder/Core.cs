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

        //Input your Client id from deloper page
        public static string ClientID = "";

        public static string version = "1.0.3";
        public static bool TurnOffAfterRecod = false;

        public static bool LivestreamerMinimize = true;

    }


    class StreamMonitor
    {
        Form1 Form1ref;

        public StreamMonitor(Form1 form1ref)
        {
            Form1ref = form1ref;
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
                ApiURL = "https://api.twitch.tv/helix/streams?user_login=" + Settings.UserName;
                request = (HttpWebRequest)WebRequest.Create(ApiURL);
                request.Headers.Add("client-id: " + Settings.ClientID);

                Stream ResponseStream;
                HttpWebResponse response;

                Form1ref.LogWrite(Settings.UserName + "의 스트림 정보를 요청합니다. ");
                string result = null;
                response = (HttpWebResponse)request.GetResponse();
                ResponseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(ResponseStream);
                result = reader.ReadToEnd();
                info = JObject.Parse(result);
                ResponseStream.Close();
                response.Close();

                if (info["data"].ToString() == "[]")
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

                Form1ref.LogWrite("ERROR:" + e.Message);
                status = Status.error;
            }

            return status;
        }

        public void LoopChack()
        {
            while (true)
            {

                Status UserStatus = ChekUser();

                Form1ref.StatusBoxUpdate(UserStatus);

                if (UserStatus == Status.NotFound)
                {
                    Form1ref.LogWrite("현재 스트림을 찾을 수 없습니다. ");

                }
                else if (UserStatus == Status.online)
                {
                    Form1ref.LogWrite("스트림이 현재 온라인 상태입니다.");
                    StartRecord();

                    StartChekProcess();
                }

                while (Form1.ProcessIsPause == true)
                {
                    Thread.Sleep(500);
                }

                Form1ref.LogWrite(Settings.Refresh.ToString() + " 초 후에 다시 확인합니다.");
                Thread.Sleep(Settings.Refresh * 1000);

            }

        }

        Process livestreamer = new Process();
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
                    Form1ref.LogWrite("주의: 두개 이상의 livestreamer 프로세스가 실행 중 입니다. ");
                }
                else if (ProcessList.Length == 0)
                {
                    Status status = ChekUser();
                    if (status == Status.online)
                    {
                        Form1ref.LogWrite("경고: Livestreamer가 알 수 없는 이유로 종료되었습니다.");
                        for (int i = 5; i > 0; i--)
                        {
                            Form1ref.LogWrite(i.ToString() + " 초 후에 재시작...");
                            Thread.Sleep(1000);
                        }
                        StartRecord();

                    }
                    else if (status == Status.NotFound)
                    {
                        Form1ref.LogWrite("스트림이 끝났습니다.");
                        Form1ref.StatusBoxUpdate(status);

                        if (Settings.TurnOffAfterRecod == true)
                        {
                            Form1ref.LogWrite("컴퓨터를 종료합니다.");

                            Process.Start("shutdown", "-s -t 50 -c " + '"' + "스트림이 끝나서 컴퓨터를 종료합니다. 컴퓨터가 종료되는 것을 원치 않으시면 35초 이내에 종료 취소 버튼을 눌러주세요" + '"');

                            Form1ref.Button3EnabledSet(true);
                        }
                        break;
                    }

                }

                Thread.Sleep(500);

            }

        }

        private void StartRecord()
        {
            string FileName = Settings.UserName + "-" + DateTime.Now.Year + "Y-" + DateTime.Now.Month + "M-"
                              + DateTime.Now.Day + "D-" + DateTime.Now.Hour + "H-" + DateTime.Now.Minute + "Min";
            Form1ref.LogWrite("파일 이름: " + FileName);

            string argmuments = "--http-header Client-ID=" + Settings.ClientID + " -o " + '"' + Settings.Directory + "/" +
                    FileName + ".mp4" + '"' + " twitch.tv/" + Settings.UserName + " " + Settings.Quality;


            Form1ref.LogWrite("시스템 호출:" + "livestreamer " + argmuments);



            livestreamer.StartInfo.FileName = "livestreamer.exe";
            livestreamer.StartInfo.Arguments = argmuments;

            if (Settings.LivestreamerMinimize == true)
            {
                livestreamer.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }
            else
            {
                livestreamer.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            }
            livestreamer.Start();
        }


    }
}

