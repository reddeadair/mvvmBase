using Gvd.Framework.Types;

namespace DepoSample
{
    /// <summary>
    /// Depo verisi. Ağacın köküdür. Bir deponun altında en çok 50 raf olur.
    /// </summary>
    /// <remarks>
    /// Yeni alan eklemek için:
    /// 1. Bu sınıfa property ekle.
    /// 2. WarehouseViewModel sınıfına aynı adla bir property ekle.
    /// 3. App projesindeki WarehouseView.xaml dosyasına bir form satırı ekle.
    /// 4. App projesindeki Resources.tr.xml dosyasına etiketi ekle.
    /// 5. Kural gerekirse WarehouseValidator sınıfına ekle.
    /// </remarks>
    public class WarehouseDto
    {
        /// <summary>
        /// Kayıt kimliği.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Depo adı. Ağaçta görünür.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Depo kodu. Ör. "D-01".
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Deponun şehri.
        /// </summary>
        public string City { get; set; } = string.Empty;
    }

    /// <summary>
    /// Raf verisi. Deponun alt düğümüdür.
    /// </summary>
    public class ShelfDto
    {
        /// <summary>
        /// Raf kodu. Ağaçta görünür. Ör. "R-01".
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Rafın taşıyabileceği en çok yük (kg). Raftaki her ürünün toplam ağırlığı bu değeri aşamaz.
        /// </summary>
        public double? MaxLoad { get; set; }
    }

    /// <summary>
    /// Ürün verisi. Rafın alt düğümüdür.
    /// </summary>
    public class ProductDto
    {
        /// <summary>
        /// Ürün kodu. Ör. "U-001".
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı. Ağaçta görünür.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Bir birimin ağırlığı (kg).
        /// </summary>
        public double? UnitWeight { get; set; }

        /// <summary>
        /// Eldeki miktar. Hesaplanan alandır: giriş miktarlarının toplamından çıkış miktarlarının toplamı çıkar.
        /// Hesap StockCalculator sınıfındadır. Formda salt okunurdur.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Stok hareketleri. Formda tablo olarak görünür.
        /// </summary>
        public List<StockMovementDto> StockMovements { get; set; } = new();
    }

    /// <summary>
    /// Stok hareketi. Ürünün tablosundaki bir satırdır.
    /// </summary>
    public class StockMovementDto : ListItemModelDtoBase
    {
        /// <summary>
        /// Hareket tarihi. Tablo hücresi "2026-11-10" ve "10.11.2026" biçimini kabul eder (Framework: NumberText).
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Hareket tipi: giriş ya da çıkış.
        /// </summary>
        public MovementType Type { get; set; } = MovementType.In;

        /// <summary>
        /// Hareket miktarı. Sıfırdan büyük olmalı.
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// Açıklama.
        /// </summary>
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stok hareketi tipi. Etiketler App projesindeki Resources.tr.xml dosyasındadır.
    /// </summary>
    public enum MovementType
    {
        In,
        Out,
    }
}
