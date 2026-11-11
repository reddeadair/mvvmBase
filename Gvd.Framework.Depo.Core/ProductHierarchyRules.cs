using Gvd.Framework.Hierarchy;
using System.Globalization;

namespace DepoSample
{
    /// <summary>
    /// Ürün ile raf arasındaki kural: ürünün toplam ağırlığı rafın en çok yükünü aşamaz.
    /// Kural kırılınca iki alan birlikte kırmızı olur: ürünün miktarı ve rafın en çok yükü.
    /// </summary>
    /// <remarks>
    /// Kural şu değişikliklerde yeniden çalışır:
    /// - ürünün miktarı (Quantity) değişince.
    /// - stok hareketleri tablosu değişince (DependsOnOwn). Miktar tablodan hesaplanır.
    /// - ürünün birim ağırlığı değişince (DependsOnOwn).
    /// - rafın en çok yükü değişince (DependsOn).
    /// Sınır: kural tek ürüne bakar. Raftaki tüm ürünlerin toplamına bakan kural yazılamaz.
    /// Neden: Framework yalnızca üst düğüm, iki üst düğüm ve çocuk sayısı kuralı verir (Framework/README.md "Bilinen sınırlar").
    /// </remarks>
    public class ProductHierarchyRules : HierarchyRules<ProductDto>
    {
        public ProductHierarchyRules()
        {
            RuleFor(product => product.Quantity)
                .WithAncestor<ShelfDto>()
                .DependsOn(shelf => shelf.MaxLoad)
                .DependsOnOwn(product => product.UnitWeight)
                // Miktar tablodan hesaplanır. Tablo değişince oturuma "StockMovements" bildirilir, "Quantity" bildirilmez.
                // AddDependency yalnızca FluentValidation kurallarını yeniden çalıştırır, bu kuralı çalıştırmaz. Bu yüzden bu satır gerekir.
                .DependsOnOwn(product => product.StockMovements)
                .Must((product, shelf) => shelf.MaxLoad == null || StockCalculator.TotalWeight(product) <= shelf.MaxLoad)
                .AlsoReportOn(shelf => shelf.MaxLoad)
                .WithMessage((product, shelf) => string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\" ürününün toplam ağırlığı ({1} kg) rafın en çok yükünü ({2} kg) aşıyor.",
                    product.Name,
                    StockCalculator.TotalWeight(product),
                    shelf.MaxLoad));
        }
    }
}
