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

        public static string version = "1.0.6";
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

        //유저의 스트림 상태를 확인하는 함수
        private Status ChekUser()
        {
            //초기 세팅
            Status status = Status.Not_determined;
            ///<summary>
            ///json result
            /// </summary>
            JObject info = null;

            try
            {
                //트위치 서버에 접속해서 방송 상태를 확인하는 요청을 보내기 위해서 request객체에 해당하는 url을 설정하고, 헤더에 clint-id를 추가함
                string ApiURL;
                HttpWebRequest request;
                ApiURL = "https://api.twitch.tv/helix/streams?user_login=" + Settings.UserName;
                request = (HttpWebRequest)WebRequest.Create(ApiURL);
                request.Headers.Add("client-id: " + Settings.ClientID);

                Stream ResponseStream;
                HttpWebResponse response;

                //콘솔에 로그를 찍고 응답을 받아서 json으로 파싱
                Form1ref.LogWrite(Settings.UserName + "의 스트림 정보를 요청합니다. ");
                string result = null;
                response = (HttpWebResponse)request.GetResponse();
                ResponseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(ResponseStream);
                result = reader.ReadToEnd();
                info = JObject.Parse(result);
                ResponseStream.Close();
                response.Close();

                //서버의 데이터가 아무것도 없으면(null이면)
                if (info["data"].ToString() == "[]")
                {
                    //NotFound상태로 설정하고
                    status = Status.NotFound;
                }
                else
                {
                    //있으면 online
                    status = Status.online;
                }

            }
            catch (Exception e)
            {
                //받아오는 과정에서 에러가 뜨면 erro로 설정하고
                Form1ref.LogWrite("ERROR:" + e.Message);
                status = Status.error;
            }

            //상태를 반환함
            return status;
        }

        //루프를 돌면서 체크함
        public void LoopChack()
        {
            while (true)
            {

                Status UserStatus = ChekUser();

                Form1ref.StatusBoxUpdate(UserStatus);

                //상태에 따라 처리 로직
                if (UserStatus == Status.NotFound)
                {
                    Form1ref.LogWrite("현재 스트림을 찾을 수 없습니다. ");

                }
                else if (UserStatus == Status.online)
                {
                    //온라인 상태이면 녹화를 시작하고
                    Form1ref.LogWrite("스트림이 현재 온라인 상태입니다.");
                    StartRecord();

                    //livestreamer프로세스를 감시한다.
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

        private void StartChekProcess()
        {
            //루프 돌면서
            while (true)
            {

                //일시정지 상태이면 휴식
                while (Form1.ProcessIsPause == true)
                {
                    Thread.Sleep(500);
                }

                //OS로부터 livestreamer이름을 가지는 프로세스 목록을 가지고 와서
                Process[] ProcessList = Process.GetProcessesByName("livestreamer");


                //리스트의 길이가 1개보다 많으면 두개 이상의 프로세스가 실행중이라고 경고하고
                if (ProcessList.Length > 1)
                {
                    Form1ref.LogWrite("주의: 두개 이상의 livestreamer 프로세스가 실행 중 입니다. ");
                } 
                else if (ProcessList.Length == 0) //리스트가 아무것도 없으면 즉 길이가 0이면
                {
                    //스트리머의 스트림 상태가 어떤지 체크하고
                    Status status = ChekUser();
                    if (status == Status.online) //아직 온라인이면
                    {
                        //경고 메세지를 로그에 출력하고
                        Form1ref.LogWrite("경고: Livestreamer가 알 수 없는 이유로 종료되었습니다.");
                        for (int i = 5; i > 0; i--)
                        {
                            //5초후에 다시 livestreamer을 실행함
                            Form1ref.LogWrite(i.ToString() + " 초 후에 재시작...");
                            Thread.Sleep(1000);
                        }
                        StartRecord();

                    }
                    else if (status == Status.NotFound) //오프라인이면
                    {
                        //스트림이 끝났다고 출력하고
                        Form1ref.LogWrite("스트림이 끝났습니다.");
                        //대시보드를 업데이트 한다
                        Form1ref.StatusBoxUpdate(status);

                        //자동 종료기능이 활성화 되어있으면
                        if (Settings.TurnOffAfterRecod == true)
                        {
                            //shutdown 명령어를 이용해서 50초 이후 자동종료 하고
                            Form1ref.LogWrite("컴퓨터를 종료합니다.");

                            Process.Start("shutdown", "-s -t 50 -c " + '"' + "스트림이 끝나서 컴퓨터를 종료합니다. 컴퓨터가 종료되는 것을 원치 않으시면 35초 이내에 종료 취소 버튼을 눌러주세요" + '"');

                            //종료 취소 버튼을 활성화 시켜준다
                            Form1ref.Button3EnabledSet(true);
                        }
                        break;
                    }

                }

                Thread.Sleep(500);

            }

        }

        Process livestreamer = new Process();
        //녹화를 livestrem 프로세스를 실행해서 녹화를 시작한다
        private void StartRecord()
        {
            //파일 이름을 구하고
            string FileName = Settings.UserName + "-" + DateTime.Now.Year + "Y-" + DateTime.Now.Month + "M-"
                              + DateTime.Now.Day + "D-" + DateTime.Now.Hour + "H-" + DateTime.Now.Minute + "Min";
            Form1ref.LogWrite("파일 이름: " + FileName);

            //livestreamer실행시에 넘겨줄 매개변수를 구한다.
            string argmuments = "--http-header Client-ID=" + Settings.ClientID + " -o " + '"' + Settings.Directory + "/" +
                    FileName + ".mp4" + '"' + " twitch.tv/" + Settings.UserName + " " + Settings.Quality;


            //실행했다고 로그를 출력한 다음
            Form1ref.LogWrite("시스템 호출:" + "livestreamer " + argmuments);


            //startinfo객체에 livestreamer을 실행하도록 이름과 매개변수를 설정한다.
            livestreamer.StartInfo.FileName = "livestreamer.exe";
            livestreamer.StartInfo.Arguments = argmuments;

            //최소화한 상태로 실행하는지 설정대로 ProcessWindowStyle의 값을 설정하고
            if (Settings.LivestreamerMinimize == true)
            {
                livestreamer.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }
            else
            {
                livestreamer.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            }

            //실행한다.
            livestreamer.Start();
        }


    }
}

