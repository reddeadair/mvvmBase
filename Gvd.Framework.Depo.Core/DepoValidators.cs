using FluentValidation;

namespace DepoSample
{
    /// <summary>
    /// Depo kuralları. Kural kırılınca alan kırmızı olur ve hata Durum panelinde görünür. Hata varken kayıt kapalıdır.
    /// </summary>
    public class WarehouseValidator : AbstractValidator<WarehouseDto>
    {
        public WarehouseValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Depo adı boş olamaz.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Depo kodu boş olamaz.");

            // Uyarı kaydı durdurmaz. Alan sarı olur.
            RuleFor(x => x.City).NotEmpty().WithSeverity(Severity.Warning).WithMessage("Şehir boş. Sevkiyat adresi eksik kalır.");
        }
    }

    /// <summary>
    /// Raf kuralları.
    /// </summary>
    public class ShelfValidator : AbstractValidator<ShelfDto>
    {
        /// <summary>
        /// Bu yükten ağır raf için uyarı verilir (kg).
        /// </summary>
        public const double HeavyLoad = 2000;

        public ShelfValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Raf kodu boş olamaz.");
            RuleFor(x => x.MaxLoad).NotNull().WithMessage("En çok yük boş olamaz.");
            RuleFor(x => x.MaxLoad).GreaterThan(0).WithMessage("En çok yük sıfırdan büyük olmalı.");
            RuleFor(x => x.MaxLoad).LessThanOrEqualTo(HeavyLoad).WithSeverity(Severity.Warning).WithMessage("En çok yük 2000 kg üstünde. Rafın taşıma belgesini denetle.");
        }
    }

    /// <summary>
    /// Ürün kuralları. Stok hareketi satırlarının kuralları da buradadır (RuleForEach).
    /// </summary>
    /// <remarks>
    /// Rafın yüküne bakan kural burada değildir. Başka düğüme bakan kural ProductHierarchyRules sınıfındadır.
    /// </remarks>
    public class ProductValidator : AbstractValidator<ProductDto>
    {
        public ProductValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Ürün kodu boş olamaz.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Ürün adı boş olamaz.");
            RuleFor(x => x.UnitWeight).NotNull().WithMessage("Birim ağırlık boş olamaz.");
            RuleFor(x => x.UnitWeight).GreaterThan(0).WithMessage("Birim ağırlık sıfırdan büyük olmalı.");
            RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).WithMessage("Miktar eksiye düştü. Çıkış hareketlerini denetle.");
            RuleForEach(x => x.StockMovements).ChildRules(movement =>
            {
                movement.RuleFor(x => x.Date).NotNull().WithMessage("Tarih boş olamaz.");
                movement.RuleFor(x => x.Quantity).NotNull().WithMessage("Miktar boş olamaz.");
                movement.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalı.");
                movement.RuleFor(x => x.Note).NotEmpty().When(x => x.Type == MovementType.Out).WithSeverity(Severity.Warning).WithMessage("Çıkış hareketine açıklama yaz.");
            });
        }
    }

    /// <summary>
    /// Stok miktarının hesabı. Ürünün Quantity alanı bu hesabın sonucudur.
    /// </summary>
    public static class StockCalculator
    {
        /// <summary>
        /// Giriş miktarlarının toplamından çıkış miktarlarının toplamını çıkarır. Boş miktar sıfır sayılır.
        /// </summary>
        /// <param name="movements">Stok hareketleri</param>
        /// <returns>Eldeki miktar</returns>
        public static int CalculateQuantity(IEnumerable<StockMovementDto> movements)
        {
            var total = 0;
            foreach (var movement in movements)
            {
                var quantity = movement.Quantity ?? 0;
                if (movement.Type == MovementType.In)
                {
                    total += quantity;
                }
                else
                {
                    total -= quantity;
                }
            }

            return total;
        }

        /// <summary>
        /// Ürünün toplam ağırlığı (kg): birim ağırlık çarpı miktar. Birim ağırlık boşsa sıfırdır.
        /// </summary>
        /// <param name="product">Ürün</param>
        /// <returns>Toplam ağırlık</returns>
        public static double TotalWeight(ProductDto product)
        {
            return (product.UnitWeight ?? 0) * product.Quantity;
        }
    }
}
