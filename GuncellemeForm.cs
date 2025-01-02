using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TarifUygulamasi
{
    public partial class GuncellemeForm : Form
    {
        
        SqlConnection baglanti=new SqlConnection("Data Source=UMUT\\SQLEXPRESS;Initial Catalog=TarifUygulamasi;Integrated Security=True");
        DataSet dataSet = new DataSet();
        SqlCommandBuilder sqlBuild;
        SqlDataAdapter adapter1;

        public GuncellemeForm()
        {
            InitializeComponent();
            
            
        }

        private void Listele()
        {
            
                //Tarifleri DataGriwWiew'e ekler
                baglanti.Open();
                adapter1 = new SqlDataAdapter("select *from Tarifler",baglanti);
                adapter1.Fill(dataSet, "Tarifler");
                dataGridView1.DataSource = dataSet.Tables["Tarifler"];
                baglanti.Close();
            

        }

        

        private void GuncellemeForm_Load(object sender, EventArgs e)
        {
            Listele();
        }

        private void GuncelleButton_Click(object sender, EventArgs e)
        {
           
            sqlBuild=new SqlCommandBuilder(adapter1);
            adapter1.Update(dataSet,"Tarifler");
            MessageBox.Show("Tarif başarıyla güncellendi.");
            
        }
    }
}
