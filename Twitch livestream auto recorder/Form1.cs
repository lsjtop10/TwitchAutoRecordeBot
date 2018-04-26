using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecoderCore;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Twitch_livestream_auto_recoder
{


    public partial class Form1 : Form
    {


        //코어부분 스레드
        Thread thread1;
        StreamMonitor monitor;
        public Form1()
        {
            InitializeComponent();
        }

        //로그를 출력하는 함수이다
        public delegate void LogWritedelegate(string info);
        public void LogWrite(string info)
        {
            if (this.InvokeRequired == true)
            {
                Invoke(new LogWritedelegate(LogWrite), info);

            }
            else
            {
                //현재 시각과 분과 매개변수로 들어온 info를 합친다.
                string message = "[" + DateTime.Now.Hour + "시 " + DateTime.Now.Minute + "분" + "]" + info;
                logbox.Items.Add(message);

                //제일 최근의 로그가 선택되도록 한다.
                int index = logbox.Items.Count - 1;
                logbox.SelectedIndex = index;

                //아이템이 1000개를 넘으면 가장 오래된 것을 지운다.
                if (logbox.Items.Count > 1000)
                {
                    logbox.Items.RemoveAt(0);
                }
            }
        }

        public delegate void StatusBoxUpdateDelegate(RecoderCore.Status status);
        public void StatusBoxUpdate(RecoderCore.Status status)
        {
            if (InvokeRequired == true)
            {
                this.Invoke(new StatusBoxUpdateDelegate(StatusBoxUpdate), status);
            }
            textBox2.Text = status.ToString();
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        public static bool ProcessIsPause = false;
        private string[] ButtonToggleTextArray = { "일시정지", "다시시작" };
        private void button1_Click(object sender, EventArgs e)
        {
            //일시정지와 다시시작 버튼을 토글 방식으로 구현
            if (ProcessIsPause == true)
            {
                ProcessIsPause = false;
                button1.Text = ButtonToggleTextArray[0];
                LogWrite("다시시작 되었습니다.");
            }
            else
            {
                ProcessIsPause = true;
                button1.Text = ButtonToggleTextArray[1];
                LogWrite("일시정지 되었습니다.");

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //설정창 생성
            Form2 dig = new Form2();

            //설정창 보여주고
            dig.ShowDialog();

            //text를 업데이트 하고
            textBox1.Text = Settings.UserName;
            textBox3.Text = Settings.Quality;
            textBox4.Text = Settings.ClientID;

            dig.Dispose();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LogWrite("프로그램이 시작되었습니다");
            LogWrite("버전: " + Settings.version);

            monitor = new StreamMonitor(this);

            button3.Enabled = false;

            if (button1.Text == ButtonToggleTextArray[0])
            {
                ProcessIsPause = false;
            }
            else
            {
                ProcessIsPause = true;
            }

            try
            {
                //프로그램 경로에 있는 settrings.txt파일 연다.
                FileStream fs = new FileStream("Settings.txt", FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);

                //설정값을 파싱
                Settings.Refresh = int.Parse(sr.ReadLine());
                Settings.UserName = sr.ReadLine();
                Settings.Quality = sr.ReadLine();
                Settings.Directory = sr.ReadLine();
                Settings.ClientID = sr.ReadLine();
                Settings.TurnOffAfterRecod = bool.Parse(sr.ReadLine());
                Settings.LivestreamerMinimize = bool.Parse(sr.ReadLine());

                //대시보드 업데이트
                textBox1.Text = Settings.UserName;
                textBox3.Text = Settings.Quality;
                textBox4.Text = Settings.ClientID;

                sr.Close();

                LogWrite("설정을 불러왔습니다.");
            }
            catch (FileNotFoundException ex)
            {
                LogWrite("Settings.txt를 찾을 수 없습니다.");

                //Settings.txt를 만들고
                FileStream fs = new FileStream("Settings.txt", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                fs.Close();

                LogWrite("Settings.txt를 생성하였습니다.");

                MessageBox.Show("초기설정을 해 주세요");

                //설정 대화 상자를 보여주고
                Form2 dig = new Form2();
                dig.ShowDialog();

                //대시보드 업데이트
                textBox1.Text = Settings.UserName;
                textBox3.Text = Settings.Quality;
                textBox4.Text = Settings.ClientID;


                dig.Dispose();
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {

            //코어는 별도의 스레드를 통해서 동작한다
            thread1 = new Thread(new ThreadStart(() => { monitor.LoopChack(); }));

            thread1.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            //프로그램이 종료될 때 livestreamer이라는 이름을 가진 프로세스는 전부 종료한다.
            Process[] ProcessList = Process.GetProcessesByName("livestreamer");

            if (ProcessList.Length > 0)
            {
                for (int i = 0; i < ProcessList.Length; i++)
                {
                    ProcessList[i].Kill();
                }
            }

            //아직 코어 스레드는 돌고 있기 때문에 종료해 준다. 
            //이 작업을 해 주지 않으면 종료가 제대로 되지 않는다.
            if (thread1 != null)
            {
                thread1.Abort();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
        
        //종료 취소 버튼의 활성화/비활성화 하는 함수이다.
        public delegate void Button3EnabledSetDelegate(bool enabled);
        public void Button3EnabledSet(bool enabled)
        {
            if (this.InvokeRequired == true)
            {
                Invoke(new Button3EnabledSetDelegate(Button3EnabledSet), enabled);
            }
            else
            {
                button3.Enabled = enabled;
            }
        }

        //종료 취소 버튼을 누르면 
        private void button3_Click(object sender, EventArgs e)
        {
            //시스템에 예약되어 있던 자동 종료를 해제시키고
            Process.Start("shutdown", "-a");

            //다시 비활성화 된다.
            button3.Enabled = false;
        }

    }
}
