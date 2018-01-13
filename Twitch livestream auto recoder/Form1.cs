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
    public delegate void StatusBoxUpdateDelegate(RecoderCore.Status status);
    public delegate void Button3EnabledSetDelegate(bool enabled);
    public delegate void ProcessInfoSetDelegate(string info);

    public partial class Form1 : Form
    {



        Thread thread1;
        StreamMonitor monitor;
        public Form1()
        {
            InitializeComponent();
        }

        public delegate void LogWritedelegate(string info);
        public void LogWrite(string info)
        {
            if (this.InvokeRequired == true)
            {
                Invoke(new LogWritedelegate(LogWrite), info);

            }
            else
            {
                string message = "[" + DateTime.Now.Hour + "시 " + DateTime.Now.Minute + "분" + "]" + info;
                logbox.Items.Add(message);

                int index = logbox.Items.Count - 1;
                logbox.SelectedIndex = index;

                if (logbox.Items.Count > 1000)
                {
                    logbox.Items.RemoveAt(0);
                }
            }
        }


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

        public void ProcessInfoSet(string info)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form2 dig = new Form2();

            dig.ShowDialog();

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
                FileStream fs = new FileStream("Settings.txt", FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);

                Settings.Refresh = int.Parse(sr.ReadLine());
                Settings.UserName = sr.ReadLine();
                Settings.Quality = sr.ReadLine();
                Settings.Directory = sr.ReadLine();
                Settings.ClientID = sr.ReadLine();
                Settings.TurnOffAfterRecod = bool.Parse(sr.ReadLine());

                textBox1.Text = Settings.UserName;
                textBox3.Text = Settings.Quality;
                textBox4.Text = Settings.ClientID;

                sr.Close();

                LogWrite("설정을 불러왔습니다.");
            }
            catch (FileNotFoundException ex)
            {
                LogWrite("Settings.txt를 찾을 수 없습니다.");

                FileStream fs = new FileStream("Settings.txt", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);

                //sw.WriteLine(Settings.Refresh.ToString());
                //sw.WriteLine(Settings.UserName);
                //sw.WriteLine(Settings.Quality);
                //sw.WriteLine(@Settings.Directory);
                //sw.WriteLine(Settings.ClientID);
                fs.Close();

                LogWrite("Settings.txt를 생성하였습니다.");

                MessageBox.Show("기본 설정을 해 주세요");

                Form2 dig = new Form2();
                dig.ShowDialog();

                textBox1.Text = Settings.UserName;
                textBox3.Text = Settings.Quality;
                textBox4.Text = Settings.ClientID;


                dig.Dispose();
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {

            thread1 = new Thread(new ThreadStart(() => { monitor.LoopChack(); }));

            thread1.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            Process[] ProcessList = Process.GetProcessesByName("livestreamer");

            if (ProcessList.Length > 0)
            {
                for (int i = 0; i < ProcessList.Length; i++)
                {
                    ProcessList[i].Kill();
                }
            }

            if (thread1 != null)
            {
                thread1.Abort();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }


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

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("shutdown", "-a");

            button3.Enabled = false;
        }

        public void ProcessInfoSet(string info)
        {

        }
    }
}
