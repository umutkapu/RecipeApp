using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TarifUygulamasi
{
    public partial class UK_Recipes : Form
    {
        
        public string conString = "Data Source=UMUT\\SQLEXPRESS;Initial Catalog=TarifUygulamasi;Integrated Security=True";
        
        public UK_Recipes()
        {
            InitializeComponent();
            LoadRecipes();
            filtreCombobox.SelectedIndex = 0; //filtrelemenin ilk indeksini ayarla
        }

        // Tarif adı ve kategorisini listBox'a ekle
        private void LoadRecipes()
        {
            string query = "SELECT TarifID, TarifAdi, HazirlamaSuresi FROM Tarifler";

            using (SqlConnection connection = new SqlConnection(conString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        int tarifID = Convert.ToInt32(reader["TarifID"]);
                        string tarifAdi = reader["TarifAdi"].ToString();
                        string hazirlamaSuresi = reader["HazirlamaSuresi"].ToString();

                        // Tarifin maliyetini hesapla
                        double toplamMaliyet = TarifMaliyetHesapla(tarifID);

                        // Tarif adını, hazırlama süresini ve maliyeti formatlayarak ListBox'a ekle
                        listBox1.Items.Add($"{tarifAdi} - Hazırlama Süresi: {hazirlamaSuresi} - Maliyet: {toplamMaliyet} TL");
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Veri yüklenirken hata: " + ex.Message);
                }
            }
        }


        private double TarifMaliyetHesapla(int tarifID)
        {
            double toplamMaliyet = 0;

            using (SqlConnection baglanti = new SqlConnection(conString))
            {
                baglanti.Open();
                string query = "SELECT tm.MalzemeMiktar, m.BirimFiyat " +
                               "FROM [Tarif_Malzeme] tm " +
                               "INNER JOIN Malzemeler m ON tm.MalzemeID = m.MalzemeID " +
                               "WHERE tm.TarifID = @TarifID";

                SqlCommand command = new SqlCommand(query, baglanti);
                command.Parameters.AddWithValue("@TarifID", tarifID);

                SqlDataReader reader = command.ExecuteReader();

                // Her malzeme için maliyet hesapla
                while (reader.Read())
                {
                    double malzemeMiktar = Convert.ToDouble(reader["MalzemeMiktar"]);
                    double birimFiyat = Convert.ToDouble(reader["BirimFiyat"]);

                    // Malzeme maliyetini hesapla ve toplam maliyete ekle
                    double malzemeMaliyeti = malzemeMiktar * birimFiyat;
                    toplamMaliyet += malzemeMaliyeti;
                }
                reader.Close();
            }

            return toplamMaliyet;
        }


        private void pictureBox2_Click(object sender, EventArgs e)
        {
           
        }

        private void AramaYap()
        {
            using (SqlConnection baglanti = new SqlConnection(conString))
            {
                baglanti.Open();
                DataTable tbl = new DataTable();
                string query = "";

                // Seçili RadioButton'a göre sorguyu oluştur
                if (radioButtonTarif.Checked)
                {
                    query = "SELECT * FROM Tarifler WHERE TarifAdi LIKE @Arama";
                }
                else if (radioButtonMalzeme.Checked)
                {
                    query = "SELECT t.TarifAdi, m.MalzemeAdi FROM Tarifler t " +
                            "INNER JOIN [Tarif_Malzeme] tm ON t.TarifID = tm.TarifID " +
                            "INNER JOIN Malzemeler m ON tm.MalzemeID = m.MalzemeID " +
                            "WHERE m.MalzemeAdi LIKE @Arama";
                }

                // Eğer sorgu boş değilse, arama işlemini yap
                if (!string.IsNullOrEmpty(query))
                {
                    SqlDataAdapter ara = new SqlDataAdapter(query, baglanti);
                    ara.SelectCommand.Parameters.AddWithValue("@Arama", "%" + textBox1.Text + "%");
                    ara.Fill(tbl);
                }

                baglanti.Close();
                dataGridView1.DataSource = tbl;
            }
        }



        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            AramaYap();

        }

        


        private void EkleButton_Click(object sender, EventArgs e)
        {
            EklemeForm eklemeForm = new EklemeForm();
            eklemeForm.ShowDialog();

            
        }

        private void GuncelleButton_Click(object sender, EventArgs e)
        {
            
            GuncellemeForm guncellemeForm = new GuncellemeForm();
            guncellemeForm.ShowDialog();

        }

        private void radioButtonMalzeme_CheckedChanged(object sender, EventArgs e)
        {
            AramaYap();
        }

        private void radioButtonTarif_CheckedChanged(object sender, EventArgs e)
        {
            AramaYap();
        }

        private void TarifSil(string tarifAdi)
        {
            using (SqlConnection connection = new SqlConnection(conString))
            {
                try
                {
                    connection.Open();

                    // Önce Tarif_Malzeme tablosundaki ilgili kayıtları sil
                    string deleteTarifMalzemeQuery = "DELETE FROM Tarif_Malzeme WHERE TarifID IN (SELECT TarifID FROM Tarifler WHERE TarifAdi = @TarifAdi)";
                    using (SqlCommand deleteTarifMalzemeCommand = new SqlCommand(deleteTarifMalzemeQuery, connection))
                    {
                        deleteTarifMalzemeCommand.Parameters.AddWithValue("@TarifAdi", tarifAdi);
                        deleteTarifMalzemeCommand.ExecuteNonQuery();
                    }

                    // Ardından Tarifler tablosundaki tarifi sil
                    string deleteTarifQuery = "DELETE FROM Tarifler WHERE TarifAdi = @TarifAdi";
                    using (SqlCommand deleteTarifCommand = new SqlCommand(deleteTarifQuery, connection))
                    {
                        deleteTarifCommand.Parameters.AddWithValue("@TarifAdi", tarifAdi);
                        int rowsAffected = deleteTarifCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Tarif başarıyla silindi.");
                        }
                        else
                        {
                            MessageBox.Show("Silinecek tarif bulunamadı.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Silme işlemi sırasında bir hata oluştu: " + ex.Message);
                }
            }
        }



        private void SilButton_Click(object sender, EventArgs e)
        {
            // Seçili item kontrolü
            if (listBox1.SelectedItem != null)
            {
                // Seçili item'dan tarif adını al
                string selectedItem = listBox1.SelectedItem.ToString();
                string tarifAdi = selectedItem.Split('-')[0].Trim(); // "Tarif Adı - Hazırlama Süresi" formatından tarif adını al

                // Tarif silme fonksiyonunu çağır
                TarifSil(tarifAdi);

                // ListBox'ı güncelle
                listBox1.Items.Clear();
                LoadRecipes();
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir tarif seçin.");
            }
        }

        private void TarifDetaylariniGetir(string tarifAdi)
        {
            using (SqlConnection connection = new SqlConnection(conString))
            {
                try
                {
                    connection.Open();

                    // Seçilen tarifin detaylarını sorgulama
                    string query = "SELECT * FROM Tarifler WHERE TarifAdi = @TarifAdi";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TarifAdi", tarifAdi);
                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            // Tarifin detaylarını formdaki alanlara doldur
                            tarifAdiDetay.Text = reader["TarifAdi"].ToString();
                            KategoriDetay.Text = reader["Kategori"].ToString();
                            SureDetay.Text = reader["HazirlamaSuresi"].ToString();
                            TalimatlarDetay.Text = reader["Talimatlar"].ToString();
                        }
                        reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Tarif detayları getirilirken bir hata oluştu: " + ex.Message);
                }
            }
        }

        private void YapilmaDurumunuKontrolEtVeMaliyetiGoster(string tarifAdi)
        {
            double gerekenMaliyet = 0;
            bool yapilabilir = true;

            using (SqlConnection connection = new SqlConnection(conString))
            {
                connection.Open();

                // Tarife ait malzemeleri ve miktarlarını sorgula
                string query = "SELECT tm.MalzemeMiktar, m.ToplamMiktar, m.BirimFiyat " +
                               "FROM [Tarif_Malzeme] tm " +
                               "INNER JOIN Malzemeler m ON tm.MalzemeID = m.MalzemeID " +
                               "INNER JOIN Tarifler t ON tm.TarifID = t.TarifID " +
                               "WHERE t.TarifAdi = @TarifAdi";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TarifAdi", tarifAdi);

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    double malzemeMiktar = Convert.ToDouble(reader["MalzemeMiktar"]);
                    double toplamMiktar = Convert.ToDouble(reader["ToplamMiktar"]);
                    double birimFiyat = Convert.ToDouble(reader["BirimFiyat"]);

                    // Eksik malzeme varsa yapılabilir durumu false yap ve maliyetini hesapla
                    if (toplamMiktar < malzemeMiktar)
                    {
                        yapilabilir = false;
                        gerekenMaliyet += (malzemeMiktar - toplamMiktar) * birimFiyat;
                    }
                }
                reader.Close();
            }

            // Checkbox'ı işaretle veya kaldır
            YapilmaKontrol.Checked = yapilabilir;

            // ListBox'ta tarifin adını, maliyeti ve yapılabilirlik durumunu göster
            if (yapilabilir)
            {
                
            }
            else
            {
                listBox1.Items.Add($"{tarifAdi} - Gereken maliyet: {gerekenMaliyet} TL");
            }
        }



        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Seçili item kontrolü
            if (listBox1.SelectedItem != null)
            {
                // Seçili item'dan tarif adını al
                string selectedItem = listBox1.SelectedItem.ToString();
                string tarifAdi = selectedItem.Split('-')[0].Trim(); // "Tarif Adı - Hazırlama Süresi" formatından tarif adını al

                // Tarifin detaylarını getir ve formdaki alanları doldur
                TarifDetaylariniGetir(tarifAdi);
                YapilmaDurumunuKontrolEtVeMaliyetiGoster(tarifAdi);
            }
        }

        private void SıralaVeYükle(string sıralamaKriteri)
        {
            listBox1.Items.Clear(); // Mevcut öğeleri temizle

            string query = "SELECT TarifAdi, HazirlamaSuresi, (SELECT SUM(m.BirimFiyat * tm.MalzemeMiktar) FROM [Tarif_Malzeme] tm " +
                           "INNER JOIN Malzemeler m ON tm.MalzemeID = m.MalzemeID WHERE tm.TarifID = t.TarifID) AS Maliyet " +
                           "FROM Tarifler t ";

            // Sıralama kriterine göre sorguyu oluştur
            switch (sıralamaKriteri)
            {
                case "Hazırlama Süresine Göre (Hızlıdan Yavaşa)":
                    query += "ORDER BY HazirlamaSuresi ASC";
                    break;
                case "Hazırlama Süresine Göre (Yavaştan Hızlıya)":
                    query += "ORDER BY HazirlamaSuresi DESC";
                    break;
                case "Maliyete Göre (Pahalıdan Ucuza)":
                    query += "ORDER BY Maliyet DESC";
                    break;
                case "Maliyete Göre (Ucuzdan Pahalıya)":
                    query += "ORDER BY Maliyet ASC";
                    break;
            }

            using (SqlConnection connection = new SqlConnection(conString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    // Tarif adı ve hazırlama süresi veya maliyet bilgilerini ListBox'a ekle
                    while (reader.Read())
                    {
                        string tarifAdi = reader["TarifAdi"].ToString();
                        string hazirlamaSuresi = reader["HazirlamaSuresi"].ToString();
                        string maliyet = reader["Maliyet"] != DBNull.Value ? reader["Maliyet"].ToString() : "0";

                        listBox1.Items.Add($"{tarifAdi} - Hazırlama Süresi: {hazirlamaSuresi} dk - Maliyet: {maliyet} TL");
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Veri yüklenirken hata: " + ex.Message);
                }
            }
        }

        private void filtreCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Seçilen sıralama kriterine göre tarifleri yükle
            SıralaVeYükle(filtreCombobox.SelectedItem.ToString());
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void filtreButton_Click(object sender, EventArgs e)
        {
            // CheckedListBox görünürlüğünü aç/kapat
            filtreCheckedListBox.Visible = !filtreCheckedListBox.Visible;

            if (filtreCheckedListBox.Visible)
            {
                // Malzemeleri doldurma işlemi
                LoadIngredientsToCheckedListBox();
            }
        }

        // CheckedListBox içine malzemeleri yüklemek için
        private void LoadIngredientsToCheckedListBox()
        {
            filtreCheckedListBox.Items.Clear(); // Önceki malzemeleri temizle

            using (SqlConnection connection = new SqlConnection(conString))
            {
                string query = "SELECT MalzemeAdi FROM Malzemeler";

                SqlCommand command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        string malzemeAdi = reader["MalzemeAdi"].ToString();
                        filtreCheckedListBox.Items.Add(malzemeAdi); // Malzemeleri ekle
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Malzemeler yüklenirken hata: " + ex.Message);
                }
            }
        }

        private void FiltrelemeIslemiYap()
        {
            List<string> secilenMalzemeler = new List<string>();

            // Seçilen malzemeleri listeye ekle
            foreach (string malzeme in filtreCheckedListBox.CheckedItems)
            {
                secilenMalzemeler.Add(malzeme);
            }

            // Seçili malzemelere göre tarifleri filtrele
            if (secilenMalzemeler.Count > 0)
            {
                listBox1.Items.Clear(); // Mevcut tarifleri temizle

                using (SqlConnection connection = new SqlConnection(conString))
                {
                    connection.Open();

                    // Sorguyu tarif başına bir kez olacak şekilde düzenle
                    string query = "SELECT t.TarifAdi, m.MalzemeAdi " +
                                   "FROM [Tarif_Malzeme] tm " +
                                   "INNER JOIN Malzemeler m ON tm.MalzemeID = m.MalzemeID " +
                                   "INNER JOIN Tarifler t ON tm.TarifID = t.TarifID " +
                                   "WHERE m.MalzemeAdi IN (" + string.Join(",", secilenMalzemeler.Select(m => "'" + m + "'")) + ")";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    // Tariflerin ve eşleşen malzemelerin listesi
                    Dictionary<string, List<string>> tarifEslesmeler = new Dictionary<string, List<string>>();
                    Dictionary<string, int> tarifToplamMalzemeSayisi = new Dictionary<string, int>();

                    while (reader.Read())
                    {
                        string tarifAdi = reader["TarifAdi"].ToString();
                        string malzemeAdi = reader["MalzemeAdi"].ToString();

                        // Tarifin eşleşen malzemeleri varsa listeye ekle
                        if (!tarifEslesmeler.ContainsKey(tarifAdi))
                        {
                            tarifEslesmeler[tarifAdi] = new List<string>();
                        }
                        tarifEslesmeler[tarifAdi].Add(malzemeAdi);
                    }
                    reader.Close();

                    // Her tarif için toplam malzeme sayısını sorgula
                    foreach (var tarif in tarifEslesmeler.Keys)
                    {
                        string countQuery = "SELECT COUNT(*) AS ToplamMalzemeSayisi " +
                                            "FROM [Tarif_Malzeme] tm " +
                                            "INNER JOIN Tarifler t ON tm.TarifID = t.TarifID " +
                                            "WHERE t.TarifAdi = @TarifAdi";

                        SqlCommand countCommand = new SqlCommand(countQuery, connection);
                        countCommand.Parameters.AddWithValue("@TarifAdi", tarif);

                        var toplamMalzemeSayisi = Convert.ToInt32(countCommand.ExecuteScalar());
                        tarifToplamMalzemeSayisi[tarif] = toplamMalzemeSayisi;
                    }

                    // ListBox'a tarif bilgilerini ekle
                    foreach (var tarif in tarifEslesmeler)
                    {
                        int eslesmeSayisi = tarif.Value.Count;
                        int toplamMalzemeSayisi = tarifToplamMalzemeSayisi[tarif.Key];
                        double eslesmeOrani = ((double)eslesmeSayisi / toplamMalzemeSayisi) * 100;

                        // Eşleşen malzemeleri virgülle ayrılmış şekilde göster
                        string eslesenMalzemeler = string.Join(", ", tarif.Value);

                        // Sonucu ListBox'a ekle
                        listBox1.Items.Add($"{tarif.Key} - Eşleşme Oranı: %{eslesmeOrani:F2} - Eşleşen Malzemeler: {eslesenMalzemeler}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen en az bir malzeme seçin.");
            }
        }




        private void filtreUygulaButton_Click(object sender, EventArgs e)
        {
            FiltrelemeIslemiYap();
        }

        
    }
}
