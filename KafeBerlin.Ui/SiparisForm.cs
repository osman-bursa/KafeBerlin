using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KafeBerlin.Data;

namespace KafeBerlin.Ui
{
    public partial class SiparisForm : Form
    {
        public event EventHandler<MasaTasindiEventArgs> MasaTasindi;

        private readonly KafeVeri _db;
        private readonly Siparis _siparis;
        BindingList<SiparisDetay> _blSiparisDetaylar;
        public SiparisForm(KafeVeri db, Siparis siparis)
        {
            _db = db;
            _siparis = siparis;
            InitializeComponent();
            dgvDetaylar.AutoGenerateColumns = false;
            MasaNoGuncelle();
            UrunleriListele();
            OdemeTutariGuncelle();
            DetaylariListele();
        }

        private void DetaylariListele()
        {
            _blSiparisDetaylar = new BindingList<SiparisDetay>(_siparis.SiparisDetaylar);
            _blSiparisDetaylar.ListChanged += _blSiparisDetaylar_ListChanged;
            dgvDetaylar.DataSource = _blSiparisDetaylar;

            //dgvDetaylar.Columns[0].HeaderText = "Ürün"; // Manuel olarak kolon isimlerini değiştirme
        }

        private void _blSiparisDetaylar_ListChanged(object sender, ListChangedEventArgs e)
        {
            OdemeTutariGuncelle();
        }

        private void OdemeTutariGuncelle()
        {
            lblOdemeTutari.Text = _siparis.ToplamTutarTL;
        }

        private void UrunleriListele()
        {
            cboUrun.DataSource = _db.Urunler;
            //cboUrun.DisplayMember = "UrunAd"; // sadece adı gösterme
        }

        private void MasaNoGuncelle()
        {
            Text = $"{_siparis.MasaNo}";
            lblMasaNo.Text = _siparis.MasaNo.ToString("00");

            cboMasaNo.Items.Clear();
            for (int i = 1; i <= _db.MasaAdet; i++)
            {
                if (!_db.MasaDoluMu(i))
                    cboMasaNo.Items.Add(i);
            }
        }

        private void btnEkle_Click(object sender, EventArgs e)
        {
            if (cboUrun.SelectedItem == null)
            {
                MessageBox.Show("Önce bir ürün seçmelisiniz");
                return;
            }

            Urun urun = (Urun)cboUrun.SelectedItem;

            _blSiparisDetaylar.Add(new SiparisDetay()
            {
                Adet = (int)nudAdet.Value,
                UrunAd = urun.UrunAd,
                BirimFiyat = urun.BirimFiyat
            });

            nudAdet.Value = 1;
        }

        private void btnAnaSayfayaDon_Click(object sender, EventArgs e)
        {
            //Close(); // formu kapatır
            DialogResult = DialogResult.Cancel;
        }

        private void btnSiparisIptal_Click(object sender, EventArgs e)
        {
            SiparisiKapat(0, SiparisDurum.Iptal);
        }

        private void btnOdemeAl_Click(object sender, EventArgs e)
        {
            SiparisiKapat(_siparis.ToplamTutar(), SiparisDurum.Odendi);
        }

        void SiparisiKapat(decimal odenenTutar, SiparisDurum durum)
        {
            string eylem = durum == SiparisDurum.Iptal ? "iptal edilecektir" : "kapatılacaktır";
            string baslik = durum == SiparisDurum.Iptal ? "İptal" : "Kapatma";

            var dr = MessageBox.Show(
                $"{_siparis.MasaNo} nolu masanın siparişi {eylem} Emin misiniz?",
                $"Sipariş {baslik} Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (dr == DialogResult.Yes)
            {
                _siparis.OdenenTutar = odenenTutar;
                _siparis.Durum = durum;
                _siparis.KapanisZamani = DateTime.Now;
                _db.AktifSiparisler.Remove(_siparis);
                _db.GecmisSiparisler.Add(_siparis);
                DialogResult = DialogResult.OK;
            }
        }

        private void btnTasi_Click(object sender, EventArgs e)
        {
            if (cboMasaNo.SelectedIndex == -1) return;
            
            int eskiMasaNo = _siparis.MasaNo;
            int hedefNo = (int)cboMasaNo.SelectedItem;
            _siparis.MasaNo = hedefNo;

            // MasaTasindi eventini invoke et
            if (MasaTasindi != null)
            {
                MasaTasindi(this, new MasaTasindiEventArgs(eskiMasaNo, hedefNo));
            }

            MasaNoGuncelle();
        }
    }
}
