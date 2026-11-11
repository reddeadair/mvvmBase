using Gvd.Framework.DependencyInjection;
using Gvd.Framework.Hierarchy;
using Gvd.Framework.Validation;

namespace DepoSample
{
    /// <summary>
    /// Depo formunun ViewModel'i. Form (WarehouseView.xaml) bu sınıfın property'lerine bağlanır.
    /// </summary>
    /// <remarks>
    /// Her property WarehouseDto içindeki aynı adlı alana bağlıdır.
    /// - Metin alanı için GenerateRefProperty kullan.
    /// - Sayı, enum ve bool alanı için GenerateValueProperty kullan.
    /// </remarks>
    public class WarehouseViewModel : NodeViewModel<WarehouseDto>
    {
        public RefProperty<string> Name { get; private set; } = null!;

        public RefProperty<string> Code { get; private set; } = null!;

        public RefProperty<string> City { get; private set; } = null!;

        /// <inheritdoc />
        protected override void InitializeProperties()
        {
            Name = GenerateRefProperty(x => x.Name);
            Code = GenerateRefProperty(x => x.Code);
            City = GenerateRefProperty(x => x.City);
        }
    }

    /// <summary>
    /// Raf formunun ViewModel'i.
    /// </summary>
    public class ShelfViewModel : NodeViewModel<ShelfDto>
    {
        public RefProperty<string> Code { get; private set; } = null!;

        public ValueProperty<double> MaxLoad { get; private set; } = null!;

        /// <inheritdoc />
        protected override void InitializeProperties()
        {
            Code = GenerateRefProperty(x => x.Code);
            MaxLoad = GenerateValueProperty(x => x.MaxLoad, min: 0, max: 100000, lsb: 0.5m);
        }
    }

    /// <summary>
    /// Ürün formunun ViewModel'i. Formda stok hareketleri tablosu da vardır.
    /// </summary>
    /// <remarks>
    /// Miktar (Quantity) hesaplanan alandır. Tablo değişince OnListContentChanged miktarı yeniden hesaplar.
    /// Liste ViewModel'i fabrikayla üretilir. Fabrikayı DepoSetup.CreateViewModelFactory verir.
    /// </remarks>
    public class ProductViewModel : NodeViewModel<ProductDto>
    {
        private readonly IViewModelFactory factory;

        public ProductViewModel(IViewModelFactory factory)
        {
            this.factory = factory;
        }

        public RefProperty<string> Code { get; private set; } = null!;

        public RefProperty<string> Name { get; private set; } = null!;

        public ValueProperty<double> UnitWeight { get; private set; } = null!;

        public ValueProperty<int> Quantity { get; private set; } = null!;

        /// <summary>
        /// Stok hareketleri tablosu. Adı DTO listesinin adıyla aynıdır. Hata yolu (StockMovements[0].Quantity) bu adı kullanır.
        /// </summary>
        public ListViewModel<StockMovementViewModel, StockMovementDto> StockMovements { get; private set; } = null!;

        /// <inheritdoc />
        protected override void InitializeProperties()
        {
            Code = GenerateRefProperty(x => x.Code);
            Name = GenerateRefProperty(x => x.Name);
            UnitWeight = GenerateValueProperty(x => x.UnitWeight, min: 0, max: 10000);
            Quantity = GenerateValueProperty(x => x.Quantity);
            Quantity.IsCalculated = true;
            StockMovements = GenerateCollection<StockMovementViewModel, StockMovementDto>(factory, x => x.StockMovements);

            // Yeni satır bugünün tarihiyle ve giriş tipiyle başlar.
            StockMovements.NewModelFactory = () => new StockMovementDto
            {
                Date = DateTime.Today,
                Type = MovementType.In,
            };
        }

        /// <inheritdoc />
        protected override void OnListContentChanged(string listName)
        {
            if (listName != nameof(ProductDto.StockMovements))
            {
                return;
            }

            // Önce DTO'yu hesapla, sonra ekranı yenile. Kurallar bu yöntemden hemen sonra çalışır ve yeni miktarı görür.
            Dto.Quantity = StockCalculator.CalculateQuantity(Dto.StockMovements);
            Quantity.SetValueFromModel(Dto.Quantity);
        }
    }

    /// <summary>
    /// Stok hareketi satırının ViewModel'i. Tablodaki her kolon bu sınıfın bir property'sidir.
    /// </summary>
    public class StockMovementViewModel : ListItemViewModel<StockMovementDto>
    {
        /// <summary>
        /// Hareket tipi açılır listesinin seçenekleri. Tablodaki ComboColumn bu listeyi kullanır.
        /// </summary>
        public static MovementType[] MovementTypes { get; } = Enum.GetValues<MovementType>();

        public ValueProperty<DateTime> Date { get; private set; } = null!;

        public ValueProperty<MovementType> Type { get; private set; } = null!;

        public ValueProperty<int> Quantity { get; private set; } = null!;

        public RefProperty<string> Note { get; private set; } = null!;

        /// <inheritdoc />
        protected override void InitializeProperties()
        {
            Date = GenerateValueProperty(Model, x => x.Date);
            Type = GenerateValueProperty(Model, x => x.Type);
            Quantity = GenerateValueProperty(Model, x => x.Quantity, min: 0);
            Note = GenerateRefProperty(Model, x => x.Note);
        }
    }
}
