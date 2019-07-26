using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Arduino
{
    public partial class Form1 : Form
    {
        //Variable define
        int BaudRate = 115200;
        int LEDPIN = 13;
        int Delay100 = 100;
        int Delay500 = 500;
        int Delay1000 = 1000;
        int Delay2000 = 2000;
        string message;
        string value;
        //static readonly object _locker = new object();
        static bool _go_B, _go_P;

        public Form1()
        {
            InitializeComponent();
            txtInfo.Visible = false;

            this.cmbSerials.Items.AddRange(SerialPort.GetPortNames());
            //MessageBox.Show("SerialPort.GetPortNames()");
            this.cmbSerials.SelectedIndex = this.cmbSerials.Items.Count - 1;//Arduino一般在最後一個接口

            this.Form1_Paint();

            this.InitialSerialPort();

            CheckSpo2Initial();
            //Kyle test
            //label1.Text = "Pulse Rate : " + TransDecToOct("11010110");
        }

        //float x0, x1, y0, y1;
        int x0_P, x1_P, y0_P, y1_P; // Pulse rate
        int x0_B, x1_B, y0_B, y1_B; // Blood oxygen
        float ratio_x, ratio_y_P;
        float wid, hei;
        float x_OP, y_OP, x_EP, y_EP;
        Bitmap bmp;
        Bitmap bmp2;

        Graphics g;
        Graphics g2;

        Pen pen_Shell;
        Pen pen_Pulse;
        Pen pen_Spo2;

        Font font_Shell;

        SolidBrush brush_Shell;
        SolidBrush brush_Pulse;
        SolidBrush brush_Spo2;

        //private void Form1_Paint(object sender, PaintEventArgs e)
        private void Form1_Paint()
        {
            try
            {
                label1.Text = "Pulse Rate : " + y1_P;
                label2.Text = "Blood Oxygen : " + y1_B;

                //----畫筆顏色----
                pen_Shell = new Pen(Color.Black, 1);
                pen_Pulse = new Pen(Color.Red, 2);
                pen_Spo2 = new Pen(Color.Green, 2);
                font_Shell = new Font("Arial", 12);
                brush_Shell = new SolidBrush(pen_Shell.Color);
                brush_Pulse = new SolidBrush(pen_Pulse.Color);
                brush_Spo2 = new SolidBrush(pen_Spo2.Color);


                //----取得picturebox寬度與高度----
                wid = pictureBox1.Width - 20;
                hei = pictureBox1.Height;
                //----是否有上一次的圖片，如果有就清除----
                if (pictureBox1.Image != null)
                    pictureBox1.Image = null;
                if (bmp != null)
                    bmp.Dispose();
                //----轉換使用者輸入的資料----
                x0_B = int.Parse("0");
                y0_B = int.Parse("0");
                x1_B = int.Parse("0");
                y1_B = int.Parse("0");

                x0_P = int.Parse("0");
                y0_P = int.Parse("0");
                x1_P = int.Parse("0");
                y1_P = int.Parse("0");

                //----計算放大倍率----
                //ratio_x = (wid - 50) / 20;
                ratio_y_P = 240 / 200;
                //----開新的Bitmap----
                bmp = new Bitmap((int)wid, (int)hei);
                //----使用上面的Bitmap畫圖----
                g = Graphics.FromImage(bmp);
                //----清除Bitmap為某顏色----
                g.Clear(Color.White);
                //----更改原點位置----
                //g.TranslateTransform(pictureBox1.Width / 2, pictureBox1.Height / 2);
                x_OP = 100;
                y_OP = hei - 50;
                x_EP = wid - x_OP;
                y_EP = hei - 50;
                g.TranslateTransform(x_OP, y_OP);
                //----畫坐標軸----
                g.DrawLine(pen_Shell, -5, 0, x_EP, 0);//x軸
                g.DrawLine(pen_Shell, 0, 5, 0, -y_EP);//y軸

                g.DrawLine(pen_Shell, x_EP, 0, x_EP - 10, 5);//x軸箭頭
                g.DrawLine(pen_Shell, x_EP, 0, x_EP - 10, -5);
                g.DrawLine(pen_Shell, 0, -y_EP, 5, -y_EP + 10);//y軸箭頭
                g.DrawLine(pen_Shell, 0, -y_EP, -5, -y_EP + 10);

                //----顯示坐標定義----
                g.DrawString("Time", font_Shell, brush_Shell, wid / 2 -54, +20);
                g.DrawString("(sec)", this.Font, brush_Shell, wid / 2 -15, +24);
                g.DrawString("Pulse", font_Shell, brush_Pulse, -95, -hei / 2);
                g.DrawString("(BPM)", this.Font, brush_Pulse, -95, -hei / 2 + 15);

                g.DrawString("Oxygen", font_Shell, brush_Spo2, -95, -hei / 2 + 40);
                g.DrawString("(%)", this.Font, brush_Spo2, -95, -hei / 2 + 55);

                for (int i = 0, j = 0; i <= 12; i++)//畫X Y軸座標位置
                {
                    j = i * 10;
                    if (i == 0) {
                        g.DrawString("0", this.Font, brush_Shell, -18, -i - 6);  // 0
                    }
                    else if (i < 5 && i > 0) {
                        g.DrawString("" + i * 20, this.Font, brush_Shell, -24, -i * 24 - 6); // 20 ~ 80
                        g.DrawLine(pen_Shell, -5, -i * 24, x_EP - 10, -i * 24);
                    }
                    else {
                        g.DrawString("" + i * 20, this.Font, brush_Shell, -30, -i * 24 - 6);  // over 100
                        g.DrawLine(pen_Shell, -5, -i * 24, x_EP - 10, -i * 24);
                    }
                    if (i < 7)
                    { 
                        g.DrawString(j.ToString().PadLeft(3, ' '), this.Font, brush_Shell, i * 60 - 9, 6);
                        g.DrawLine(pen_Shell, i * 60, 0, i * 60, +5);
                    }
                }

                //----將Bitmap顯示在Picture上
                pictureBox1.Image = bmp;
                //bmp2 = bmp;
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);//錯誤視窗
            }
            finally
            {
                GC.Collect();//清除垃圾
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    _go_B = false;

                    timer1.Start();

                    this.txtInfo.Text = "";

                    port.WriteLine("1");
                    for (int abc = 0; abc < 10000000; abc++) { }
                    //Thread.Sleep(Delay100);  // delay 100 ms

                    //port.WriteLine("2");
                    //for (int abc = 0; abc < 10000000; abc++) { }
                    //Thread.Sleep(Delay1000);  // delay 100 ms

                    port.WriteLine("5");
                    for (int abc = 0; abc < 10000000; abc++) { }

                    port.WriteLine("4");
                    for (int abc = 0; abc < 10000000; abc++) { }

                    _go_P = true;
                    PulseRate();
                    //get_PulseRate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Button fail：" + ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    //Form1_Paint();
                    _go_P = false;

                    timer1.Start();
                    this.txtInfo.Text = "";

                    port.WriteLine("1");
                    //label1.Text = "Send : 1 = FindSlaveAddress";
                    for (int abc = 0; abc < 10000000; abc++) { }
                    //port.WriteLine("2");
                    //label2.Text = "Send : 2 = CheckBoardInitialDone";
                    //for (int abc = 0; abc < 10000000; abc++) { }
                    port.WriteLine("5");
                    //label1.Text = "Send : 5 = StopMeasure";
                    for (int abc = 0; abc < 10000000; abc++) { }
                    port.WriteLine("4");
                    //label1.Text = "Send : 4 = StartMeasure";
                    for (int abc = 0; abc < 10000000; abc++) { }

                    _go_B = true;
                    BloodOxygen();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Button2 fail：" + ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Button3_Click_1(object sender, EventArgs e)
        {
            _go_P = _go_B = false;
            timer1.Stop();
        }

        //static void DoSomething()
        private async void PulseRate()
        {
            while (_go_P)
            {
                await Task.Run(() =>
                {
                    Thread.Sleep(Delay500);
                });
                //Finish();
                this.get_PulseRate();
            }
        }

        private async void BloodOxygen()
        {
            while (_go_B)
            {
                    await Task.Run(() =>
                    {
                        Thread.Sleep(Delay500);
                    });
                    //Finish();
                    this.get_BloodOxygen();
            }
        }

        private void get_PulseRate()
        {
            if (port != null && port.IsOpen)
            {
                port.WriteLine("7");
            }
        }

        private void get_BloodOxygen()
        {
            if (port != null && port.IsOpen)
            {
                port.WriteLine("6");
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            //g.DrawLine(pen, 0, 0, i++, i++);
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            x1_B = x1_P = 0;
            this.txtInfo.Text = "";
            g.Clear(Color.White);
            Form1_Paint();
        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.RefreshInfoTextBox();
        }
        
        private SerialPort port = null;

        private void Timer1_Tick_1(object sender, EventArgs e)
        {
            //label1.Text = "Pulse Rate : " + TransDecToOct("11010110");

            //y1_P = ;
            label1.Text = "Pulse Rate : " + y1_P;
            label2.Text = "Blood Oxygen : " + y1_B;

            string[] coordinate;
            //if (System.Text.RegularExpressions.Regex.IsMatch(value, "[0-9]"))
            {
                try
                {
                    if (value.Contains("0x01"))
                    {
                        coordinate = value.Split(new char[2] { ':', '.' });
                        ++x1_P;
                        y1_P = Convert.ToInt32(coordinate[1], 2);// kyle test

                        if (y1_P >= 0)
                        {
                            //g.DrawLine(pen_Pulse, x0_P, -y0_P, x1_P, -y1_P * ratio_y_P);
                            //g.DrawLine(pen_Pulse, x0_P, -y0_P, x1_P, -y1_P * ratio_y_P);
                            g.DrawLine(pen_Pulse, x0_P, -y0_P * 240 / 200, x1_P, -y1_P * 240 / 200);
                            y0_P = y1_P;
                            x0_P = x1_P;
                        }
                    }
                    else if (value.Contains("0x02"))
                    {
                        coordinate = value.Split(new char[2] { ':', '.' });
                        ++x1_B;
                        //y1_B = Convert.ToInt32(coordinate[1], 2);// kyle test
                        y1_B = TransDecToOct(coordinate[1]);

                        //MessageBox.Show("" + y1_B);

                        if (y1_B >= 0)
                        {
                            g.DrawLine(pen_Spo2, x0_B, -y0_B * 240 / 200, x1_B, -y1_B * 240 / 200);
                            y0_B = y1_B;
                            x0_B = x1_B;
                        }
                    }
                }
                catch
                {
                    //MessageBox.Show("認不到可識別的字串");
                }
            }

            if ((x1_B >= x_EP) || (x1_P >= x_EP))
            {
                x1_B = x1_P = 0;
                Form1_Paint();
                //g.Clear(Color.White);
                //bmp2 = bmp;
            }

            //pictureBox1.Image = null;
            //g.Clear(Color.White);
            pictureBox1.Image = bmp;

        }

        private void CheckSpo2Initial()
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    for (int abc = 0; abc < 10000000; abc++) { }
                    port.WriteLine("2");    // Check board Initial Done: oCare M1 initialize done
                    for (int abc = 0; abc < 10000000; abc++) { }
                    if (value.Contains("0x03"))
                    {
                        string[] coordinate = value.Split(new char[2] { ':', '.' });
                        if (coordinate.Length > 1)
                        {
                            //MessageBox.Show("" + coordinate[1]);
                            //coordinate[1] & bit
                            button1.Enabled = true;
                            button2.Enabled = true;
                        }
                        else
                        {
                            button1.Enabled = false;
                            button2.Enabled = false;
                        }
                    }
                }
            }
            catch
            {
                button1.Enabled = false;
                button2.Enabled = false;
                MessageBox.Show("Spo2 board is not ready");
            }

            //return -1;
        }

        private void InitialSerialPort()
        {
            try
            {
                string portName = this.cmbSerials.SelectedItem.ToString();
                port = new SerialPort(portName, BaudRate);
                port.Encoding = Encoding.ASCII;
                port.DataReceived += port_DataReceived;
                port.Open();
                this.ChangeArduinoSendStatus(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化串口發生錯誤：" + ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

 
/// <summary>
/// 關閉並销毁串口實例
/// </summary>
private void DisposeSerialPort()
        {
            if (port != null)
            {
                try
                {
                    this.ChangeArduinoSendStatus(false);
                    if (port.IsOpen)
                    {
                        port.Close();
                    }
                    port.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("關閉串口發生錯誤：" + ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// 改變Arduino串口的發送狀態
        /// </summary>
        /// <param name="allowSend">是否允许發送數據</param>
        private void ChangeArduinoSendStatus(bool allowSend)
        {
            if (port != null && port.IsOpen)
            {
                if (allowSend)
                {
                    //port.WriteLine("H");
                    //port.WriteLine("serial start");
                }
                else
                {
                    //port.WriteLine("L");
                    //port.WriteLine("serial stop");
                }
            }
        }

        /// 從串口讀取數據並轉換為字串形式
        /// </summary>
        /// <returns></returns>
        private string ReadSerialData()
        {
            //string value = "";
            value = "";
            try
            {
                if (port != null && port.BytesToRead > 0)
                {
                    value = port.ReadExisting();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("讀取串口數據發生錯誤：" + ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return value;
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            //txtInfo.AppendText("..." + "\r\n");
            this.txtInfo.ScrollBars = ScrollBars.Vertical;
            this.txtInfo.SelectionStart = txtInfo.Text.Length;
            this.txtInfo.ScrollToCaret();
        }

        /// <summary>
        /// 在讀取到數據時候刷新文本框的信息
        /// </summary>
        private void RefreshInfoTextBox()
        {
//            string value = this.ReadSerialData();
            value = this.ReadSerialData();

            Action<string> setValueAction = text => this.txtInfo.Text += text;

            if (this.txtInfo.InvokeRequired)
            {
                this.txtInfo.Invoke(setValueAction, value);
                // Kyle : transfer the coordinates from String to INT
            }
            else
            {
                //MessageBox.Show("認不到可識別的字串_!");
                setValueAction(value);
            }
        }

        int TransDecToOct(string a)
        {
            //string s_temp = "11010110";
            try
            {
                int v_temp = 0, v2_temp = 0;
                //v_temp = Convert.ToInt32(a, 2); // trans 2's to 10's
                //v_temp = Convert.ToInt32(v_temp, 2);
                string s2_temp = string.Format("{0:X}",Convert.ToInt32(a, 2));  // trans 2's to 16's
                                                                                //MessageBox.Show("" + s2_temp.Length);   // debug

                if (s2_temp.Length > 1)
                {
                    for (int i = 0; i < 2; i++)
                    {
                    //    char letter = array[i];
                    //    v2_temp = Convert.ToInt32(letter);
                    //MessageBox.Show("" + v2_temp); //okay

                        v2_temp = Convert.ToInt32(s2_temp[i]);
                        if (v2_temp > 64)
                            v2_temp -= 55;
                        else if (v2_temp >= 48 && v2_temp <= 57)
                            v2_temp -= 48;
                        else
                            v2_temp = 0;

                        if (i == 0)
                            v_temp = v2_temp * 8;
                        else
                            v_temp += v2_temp;
                    }
                }
                else
                    return 0;

                return v_temp;
            }
            catch
            {
                MessageBox.Show("out of range");
                return -1;
            }
            
        }

    }
}
