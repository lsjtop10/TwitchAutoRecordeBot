using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using RecoderCore;

namespace Twitch_livestream_auto_recoder
{
    public partial class Form2 : Form
    {

        private bool FirstRun;
        public Form2()
        {
            InitializeComponent();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            textBox3.Text = dialog.SelectedPath;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 5)
            {
                comboBox1.DropDownStyle = ComboBoxStyle.DropDown;
            }
            else
            {
                comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "" || textBox3.Text == "")
            {
                MessageBox.Show("오류: 설정을 적용할 수 없습니다. 다음의 사항을 빠짐없이 입력했는지 확인해 주세요" +
                                 "\n 1.스트리머 ID" +
                                 "\n 2.Client ID" +
                                 "\n 3.저장경로" +
                                 "\n 4.새로고침 시간");
                return;
            }

            try
            {
                Settings.UserName = textBox1.Text;
                Settings.Directory = @textBox3.Text;
                Settings.ClientID = textBox2.Text;
                Settings.TurnOffAfterRecod = checkBox1.Checked;
                Settings.LivestreamerMinimize = checkBox2.Checked;

                if (comboBox1.Text == "160p (worst)")
                {
                    Settings.Quality = "worst";
                }
                else if (comboBox1.Text == "720p (best)")
                {
                    Settings.Quality = "best";
                }
                else
                {
                    Settings.Quality = comboBox1.Text;
                }

                bool ParseSucces = int.TryParse(textBox4.Text, out Settings.Refresh);

                if (ParseSucces == false)
                {
                    MessageBox.Show("오류: 설정을 적용할 수 없습니다. 새로고침 시간에 허용할 수 없는 문자가 있습니다");
                    return;
                }
                else
                {
                    if (Settings.Refresh < 0)
                    {
                        MessageBox.Show("오류: 설정을 적용할 수 없습니다. 음수값은 허용되지 않습니다.");

                        textBox4.Text = "";
                        Settings.Refresh = 0;
                        return;
                    }
                }

                FileStream fs = new FileStream("Settings.txt", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);

                sw.WriteLine(Settings.Refresh.ToString());
                sw.WriteLine(Settings.UserName);
                sw.WriteLine(Settings.Quality);
                sw.WriteLine(@Settings.Directory);
                sw.WriteLine(Settings.ClientID);
                sw.WriteLine(Settings.TurnOffAfterRecod);
                sw.WriteLine(Settings.LivestreamerMinimize);
                sw.Close();

                if ((Button)sender == button2)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류: 설정을 적용하는 중에 오류가 발생했습니다.\n오류 메세지:" +
                                ex.Message);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            textBox1.Text = Settings.UserName;
            textBox2.Text = Settings.ClientID;
            textBox3.Text = Settings.Directory;
            textBox4.Text = Settings.Refresh.ToString();
            checkBox1.Checked = Settings.TurnOffAfterRecod;
            checkBox2.Checked = Settings.LivestreamerMinimize;

            //모든 값이 비어있으면 설정이 아무것도 되어 있지 않은 상태
            if (Settings.UserName == "" &&
               Settings.ClientID == "" &&
               Settings.Directory == "")
            {
                FirstRun = true;
            }

            checkBox1.Checked = Settings.TurnOffAfterRecod;

            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                if (Settings.Quality == "")
                {
                    comboBox1.SelectedIndex = 4;
                    break;
                }

                comboBox1.SelectedIndex = i;

                if (comboBox1.SelectedItem.ToString() == Settings.Quality)
                {
                    comboBox1.SelectedIndex = i;
                    break;
                }
                else if (Settings.Quality == "worst")
                {
                    comboBox1.SelectedIndex = 1;
                    break;
                }
                else if (Settings.Quality == "best")
                {
                    comboBox1.SelectedIndex = 4;
                    break;
                }
                else if (i == 5)
                {
                    comboBox1.SelectedIndex = 5;
                    comboBox1.Text = Settings.Quality;
                }

            }

        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((textBox1.Text == "" || textBox2.Text == "" || textBox3.Text == "") && FirstRun == true)
            {
                MessageBox.Show("경고: 초기설정이 완료되지 않았습니다. 초기설정을 완료해 주세요");
                return;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }
    }
}
