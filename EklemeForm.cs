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
    public partial class EklemeForm : Form
    {

        public string conString = "Data Source=UMUT\\SQLEXPRESS;Initial Catalog=TarifUygulamasi;Integrated Security=True";
        public EklemeForm()
        {
            InitializeComponent();
        }

        private void EklemeForm_Load(object sender, EventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(conString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT MalzemeID, MalzemeAdi FROM Malzemeler";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            // Malzeme ComboBox'ına ekle
                            cmbMalzeme1.Items.Add(new KeyValuePair<int, string>((int)reader["MalzemeID"], reader["MalzemeAdi"].ToString()));
                            cmbMalzeme2.Items.Add(new KeyValuePair<int, string>((int)reader["MalzemeID"], reader["MalzemeAdi"].ToString()));
                            cmbMalzeme3.Items.Add(new KeyValuePair<int, string>((int)reader["MalzemeID"], reader["MalzemeAdi"].ToString()));
                            // İhtiyaca göre daha fazla ComboBox ekleyin
                        }
                        reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bir hata oluştu: " + ex.Message);
                }
            }

            // ComboBox'ta sadece malzeme adını göster
            cmbMalzeme1.DisplayMember = "Value";
            cmbMalzeme1.ValueMember = "Key";
            cmbMalzeme2.DisplayMember = "Value";
            cmbMalzeme2.ValueMember = "Key";
            cmbMalzeme3.DisplayMember = "Value";
            cmbMalzeme3.ValueMember = "Key";
        }



        //Duplicate Kontrolü
        private bool TarifVarMi(string tarifAdi)
        {
            using (SqlConnection connection = new SqlConnection(conString))
            {
                try
                {
                    connection.Open();

                    // Aynı isimde tarif var mı kontrol et
                    string query = "SELECT COUNT(*) FROM Tarifler WHERE TarifAdi = @TarifAdi";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TarifAdi", tarifAdi);
                        int count = (int)command.ExecuteScalar();

                        // Eğer kayıt varsa, true döndür
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    return false;
                }
            }
        }

        private void EkleButton_Click(object sender, EventArgs e)
        {
            string tarifAdi = txtTarifAdi.Text;

            // Öncelikle tarifin zaten var olup olmadığını kontrol et
            if (TarifVarMi(tarifAdi))
            {
                MessageBox.Show("Bu isimde bir tarif zaten mevcut.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(conString))
            {
                try
                {
                    connection.Open();

                    // Tarif ekleme sorgusu
                    string insertTarifQuery = "INSERT INTO Tarifler (TarifAdi, Kategori, HazirlamaSuresi, Talimatlar) OUTPUT INSERTED.TarifID VALUES (@TarifAdi, @Kategori, @HazirlamaSuresi, @Talimatlar)";

                    using (SqlCommand command = new SqlCommand(insertTarifQuery, connection))
                    {
                        command.Parameters.AddWithValue("@TarifAdi", tarifAdi);
                        command.Parameters.AddWithValue("@Kategori", cmbKategori.SelectedItem);
                        command.Parameters.AddWithValue("@HazirlamaSuresi", int.Parse(txtHazirlamaSuresi.Text));
                        command.Parameters.AddWithValue("@Talimatlar", txtTalimatlar.Text);

                        // Tarif ekle ve eklenen tarifin ID'sini al
                        int insertedTarifID = (int)command.ExecuteScalar();

                        // Seçilen malzemeleri Tarif-Malzeme İlişkisi tablosuna ekle
                        if (cmbMalzeme1.SelectedItem != null)
                            MalzemeIliskisiniEkle(connection, insertedTarifID, (KeyValuePair<int, string>)cmbMalzeme1.SelectedItem, txtMalzeme1Miktar.Text);

                        if (cmbMalzeme2.SelectedItem != null)
                            MalzemeIliskisiniEkle(connection, insertedTarifID, (KeyValuePair<int, string>)cmbMalzeme2.SelectedItem, txtMalzeme2Miktar.Text);

                        if (cmbMalzeme3.SelectedItem != null)
                            MalzemeIliskisiniEkle(connection, insertedTarifID, (KeyValuePair<int, string>)cmbMalzeme3.SelectedItem, txtMalzeme3Miktar.Text);

                        MessageBox.Show("Tarif ve malzemeler başarıyla eklendi.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bir hata oluştu: " + ex.Message);
                }
            }
        }

        private void MalzemeIliskisiniEkle(SqlConnection connection, int tarifID, KeyValuePair<int, string> selectedMalzeme, string miktar)
        {
            string insertRelationQuery = "INSERT INTO Tarif_Malzeme (TarifID, MalzemeID, MalzemeMiktar) VALUES (@TarifID, @MalzemeID, @MalzemeMiktar)";
            

            using (SqlCommand command = new SqlCommand(insertRelationQuery, connection))
            {
                command.Parameters.AddWithValue("@TarifID", tarifID);
                command.Parameters.AddWithValue("@MalzemeID", selectedMalzeme.Key);
                command.Parameters.AddWithValue("@MalzemeMiktar", float.Parse(miktar));

                command.ExecuteNonQuery();
                
            }
            
        }





        private void YeniMalzemeButton_Click(object sender, EventArgs e)
        {
            // Malzeme bilgilerini al
            string malzemeAdi = txtMalzemeAdi.Text; // Malzeme adı TextBox'ı
            string toplamMiktar = txtToplamMiktar.Text; // Malzeme toplam miktarı TextBox'ı
            string malzemeBirim = cmbMalzemeBirim.SelectedItem.ToString(); // Malzeme birimi ComboBox'ı
            float birimFiyat = float.Parse(txtBirimFiyat.Text); // Birim fiyatı TextBox'ı

            // Boş kontrolü yap
            if (string.IsNullOrEmpty(malzemeAdi) || string.IsNullOrEmpty(toplamMiktar) || string.IsNullOrEmpty(malzemeBirim) || birimFiyat <= 0)
            {
                MessageBox.Show("Tüm malzeme bilgilerini doldurun ve geçerli bir birim fiyat girin.");
                return;
            }

            // Veritabanına bağlan ve malzemeyi ekle
            using (SqlConnection connection = new SqlConnection(conString))
            {
                try
                {
                    connection.Open();

                    // Malzeme ekleme sorgusu
                    string insertQuery = "INSERT INTO Malzemeler (MalzemeAdi, ToplamMiktar, MalzemeBirim, BirimFiyat) VALUES (@MalzemeAdi, @ToplamMiktar, @MalzemeBirim, @BirimFiyat)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MalzemeAdi", malzemeAdi);
                        command.Parameters.AddWithValue("@ToplamMiktar", toplamMiktar);
                        command.Parameters.AddWithValue("@MalzemeBirim", malzemeBirim);
                        command.Parameters.AddWithValue("@BirimFiyat", birimFiyat);

                        int rowsAffected = command.ExecuteNonQuery();

                        // Başarılı ekleme mesajı
                        MessageBox.Show($"{rowsAffected} malzeme başarıyla eklendi.");

                        MalzemeComboBoxlariGuncelle(connection);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bir hata oluştu: " + ex.Message);
                }
            }

            // Girdileri temizle
            txtMalzemeAdi.Clear();
            txtToplamMiktar.Clear();
            txtBirimFiyat.Clear();
        }

        // ComboBox'ları güncelleyen fonksiyon
        private void MalzemeComboBoxlariGuncelle(SqlConnection connection)
        {
            try
            {
                // Öncelikle mevcut malzemeleri ComboBox'lardan temizleyin
                cmbMalzeme1.Items.Clear();
                cmbMalzeme2.Items.Clear();
                cmbMalzeme3.Items.Clear();

                // Malzemeleri tekrar yükleyin
                string query = "SELECT MalzemeID, MalzemeAdi FROM Malzemeler";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        KeyValuePair<int, string> malzeme = new KeyValuePair<int, string>((int)reader["MalzemeID"], reader["MalzemeAdi"].ToString());
                        cmbMalzeme1.Items.Add(malzeme);
                        cmbMalzeme2.Items.Add(malzeme);
                        cmbMalzeme3.Items.Add(malzeme);
                    }
                    reader.Close();
                }

                // ComboBox'ta sadece malzeme adını göster
                cmbMalzeme1.DisplayMember = "Value";
                cmbMalzeme1.ValueMember = "Key";
                cmbMalzeme2.DisplayMember = "Value";
                cmbMalzeme2.ValueMember = "Key";
                cmbMalzeme3.DisplayMember = "Value";
                cmbMalzeme3.ValueMember = "Key";
            }
            catch (Exception ex)
            {
                MessageBox.Show("ComboBox güncellenirken bir hata oluştu: " + ex.Message);
            }
        }

        
    }
}
