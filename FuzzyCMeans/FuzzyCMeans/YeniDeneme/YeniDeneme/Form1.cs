using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using ExcelDataReader;

namespace FuzzyCMeans
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        DataSet result;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workbook|*.xls", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    FileStream fs = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read);
                    IExcelDataReader reader = ExcelReaderFactory.CreateBinaryReader(fs);

                    result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });
                    comboBox1.Items.Clear();
                    foreach (DataTable dt in result.Tables)
                    {
                        comboBox1.Items.Add(dt.TableName);
                    }
                    reader.Close();
                    comboBox1.SelectedIndex = 0;
                    cbRandom.Checked = true;
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.DataSource = result.Tables[comboBox1.SelectedIndex];
            Point_Ciz(comboBox1.SelectedIndex);

        }

        private void Point_Ciz(int index)
        {
            chart1.Series.Clear();
            chart1.Series.Add("Veriler");
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                chart1.Series["Veriler"].Points.Add(new DataPoint(Convert.ToDouble(dataGridView1[0, i].Value.ToString()), Convert.ToDouble(dataGridView1[1, i].Value.ToString())));
            }
            chart1.Series["Veriler"].ChartType = SeriesChartType.Point;
            //chart1.Series["Veriler"].MarkerStyle = MarkerStyle.Square;
            //chart1.Series["Veriler"].MarkerSize = 15;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            int itesayar = 1;
            bool islemsizFlag = false;
            int kumeSayisi = Convert.ToInt32(kumeSayisiTB.Text);
            double parametre = Convert.ToDouble(parametreTB.Text);
            double epsilon = Convert.ToDouble(epsilonTB.Text);

            if (kumeSayisi > dataGridView1.RowCount)
            {
                kumeSayisi = dataGridView1.RowCount;
                kumeSayisiTB.Text = kumeSayisi.ToString();
            }

            if (!(parametre > 1))
            {
                parametre = 2;
                parametreTB.Text = parametre.ToString();
                islemsizFlag = true;
                goto islemsiz;
            }

            if (parametre > 30)
            {
                parametre = 30;
                parametreTB.Text = parametre.ToString();
                islemsizFlag = true;
                goto islemsiz;
            }


            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                X.Add(Convert.ToDouble(dataGridView1[0, i].Value));
                Y.Add(Convert.ToDouble(dataGridView1[1, i].Value));
            }
            double[,] Uyelik = new double[kumeSayisi, dataGridView1.RowCount];
            double[,] distance = new double[kumeSayisi, dataGridView1.RowCount];
            double[,] Merkezler = new double[kumeSayisi, 2];

            if(cbRandom.Checked)
            {
                //random
                Random rnd = new Random();
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    for (int j = 0; j < kumeSayisi; j++)
                    {
                        Uyelik[j, i] = 0;
                    }
                    Uyelik[rnd.Next(kumeSayisi),i] = 1;
                }
            }
            else
            {
                //manuel
                for (int i = 0; i < kumeSayisi; i++)
                {
                    for (int j = 0; j < dataGridView1.RowCount; j++)
                    {
                        Uyelik[i, j] = 0;
                    }
                    Uyelik[i, i] = 1;
                }
                for (int i = kumeSayisi; i < dataGridView1.RowCount; i++)
                {
                    Uyelik[kumeSayisi - 1, i] = 1;
                }
            }

            
            dondur:
            double nuToplam = 0, temp = 0, toplamX = 0, toplamY = 0;
            //kume merkezleri
            for (int i = 0; i < kumeSayisi; i++)
            {
                nuToplam = 0;
                toplamX = 0;
                toplamY = 0;
                for (int k = 0; k < dataGridView1.RowCount; k++)
                {
                    temp = Math.Pow(Uyelik[i, k], parametre);
                    toplamX += temp * X[k];
                    toplamY += temp * Y[k];
                    nuToplam += temp;
                }
                Merkezler[i, 0] = toplamX / nuToplam;
                Merkezler[i, 1] = toplamY / nuToplam;
            }

            //distance matrisi küme merkezine uzaklıklar
            for (int i = 0; i < kumeSayisi; i++)
            {
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    distance[i, j] = Math.Sqrt(Math.Pow((Merkezler[i, 0] - X[j]), 2) + Math.Pow((Merkezler[i, 1] - Y[j]), 2));
                }
            }
            

            //ayırma matrisi
            double kulToplam = 0;
            bool flag = true;
            double[,] Uyelik2 = new double[kumeSayisi, dataGridView1.RowCount];
            for (int i = 0; i < kumeSayisi; i++)
            {
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    kulToplam = 0; flag = true;
                    for (int k = 0; k < kumeSayisi; k++)
                    {
                        if (distance[i, j] != 0 && distance[k, j] != 0)
                        {
                            kulToplam += Math.Pow((distance[i, j] / distance[k, j]), (2 / (parametre - 1)));
                        }
                        else if (distance[i, j] == 0 && distance[k, j] == 0)
                        {
                            kulToplam += 1;
                        }
                        else if (distance[i, j] != 0 && distance[k, j] == 0)
                        {
                            kulToplam = 0;
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        kulToplam = 1 / kulToplam;
                    }

                    Uyelik2[i, j] = kulToplam;
                }
            }

            //epsilon kontrol
            bool bayrak = true;
            for (int i = 0; i < kumeSayisi; i++)
            {
                if (!bayrak)
                    break;
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    if (epsilon < Math.Abs(Uyelik[i, j] - Uyelik2[i, j]))
                    {
                        bayrak = false;
                        break;
                    }
                }
            }

            Uyelik = Uyelik2;

            //iterasyon değeri kısmı
            double jdegeri = 0;
            for (int i = 0; i < kumeSayisi; i++)
            {
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    jdegeri += Math.Pow(Uyelik[i, j], parametre) * Math.Pow(distance[i, j], 2);
                }
            }

            richTextBox1.Text += itesayar + ". İterasyon J = " + jdegeri + "\n";
            itesayar++;


            //donuyor mu kontrol
            if (!bayrak)
                goto dondur;



            chart1.Series.Clear();

            List<string> kumeIsımler = new List<string>();
            kumeIsımler.Add("Merkezler");
            for (int i = 1; i < kumeSayisi + 1; i++)
            {
                kumeIsımler.Add("Kume " + i);
            }
            foreach (var item in kumeIsımler)
            {
                chart1.Series.Add(item);

            }
            chart1.Series[kumeIsımler[0]].MarkerStyle = MarkerStyle.Square;
            chart1.Series[kumeIsımler[0]].MarkerSize = 15;


            //kume kararı
            double buyukKume = 0;
            int indis = 0;
            int indis2 = 0;
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                buyukKume = 0; indis = 0; indis2 = 0;
                for (int j = 0; j < kumeSayisi; j++)
                {
                    if (Uyelik[j, i] > buyukKume)
                    {
                        buyukKume = Uyelik[j, i];
                        indis = j;
                        indis2 = i;
                    }
                }
                chart1.Series[indis + 1].Points.Add(new DataPoint(X[indis2], Y[indis2]));
            }

            //merkezler eklenir
            for (int i = 0; i < kumeSayisi; i++)
            {
                chart1.Series[kumeIsımler[0]].Points.Add(new DataPoint(Merkezler[i, 0], Merkezler[i, 1]));
            }

            //grafik şekli seçiliyor
            for (int i = 1; i < kumeIsımler.Count(); i++)
            {
                chart1.Series[kumeIsımler[i]].ChartType = SeriesChartType.Point;
            }
            chart1.Series[kumeIsımler[0]].ChartType = SeriesChartType.Point;
            


            islemsiz:
            if(islemsizFlag)
            {
                MessageBox.Show("m' Parametre 1'den büyük,30'dan küçük olmalı");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
        }
    }
}