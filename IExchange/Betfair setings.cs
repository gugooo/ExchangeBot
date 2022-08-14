using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Setings = IExchange.Properties.Settings;

namespace IExchange
{
    public partial class Betfair_setings : Form
    {
        private Betfair ptr;
        public Betfair_setings(Betfair _BF)
        {
            InitializeComponent();
            username.Text = Setings.Default.Username;
            password.Text = Setings.Default.Password;
            AppKey.Text = Setings.Default.AppKey;
            CertP12.Text = Setings.Default.Cert_P12;
            CertExpKey.Text = Setings.Default.CertExpKey;
            SQL_Server_Name.Text = Setings.Default.SQL;
            ptr = _BF;
            MessBox.Text = "Press Login...";
        }

        private void Betfair_setings_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Visible = false;
            e.Cancel = true;
            Setings.Default.Username = username.Text;
            Setings.Default.Password = password.Text;
            Setings.Default.AppKey = AppKey.Text;
            Setings.Default.Cert_P12 = CertP12.Text;
            Setings.Default.CertExpKey = CertExpKey.Text;
            Setings.Default.SQL = SQL_Server_Name.Text;
            Setings.Default.Save();
        }

        public async void button1_Click(object sender, EventArgs e)
        {
            ptr.AppKey = AppKey.Text;
            ptr.Certificat_P12 = this.CertP12.Text;
            ptr.Certificat_ExportKey = this.CertExpKey.Text;
            try
            {
                this.MessBox.Text +=await ptr.Login(this.username.Text, this.password.Text);
            }
            catch (Exception ex)
            {
                this.MessBox.Text += "\nLogin Error[1]:" + ex.Message;

            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                this.MessBox.Text += await ptr.Logout();
            }
            catch (Exception ex)
            {
                this.MessBox.Text += "\nLog Out Error[2]:" + ex.Message;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Certificat P12 Files|*.p12";
            openFileDialog1.Title = "Select a Certificat P12 File";
            openFileDialog1.FileName = "Certificat_P12";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.CertP12.Text = openFileDialog1.FileName;
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
    }
}
