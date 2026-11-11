using Gvd.Framework.Hierarchy;
using Gvd.Framework.Services;
using System.Globalization;

namespace DepoSample
{
    /// <summary>
    /// Bellek içi kayıt katmanı. Program kapanınca kayıtlar kaybolur. Gerçek proje bu iki arayüzü veritabanıyla yazar.
    /// </summary>
    /// <remarks>
    /// Yükleme ve kayıt bir kopya kullanır. Neden: form DTO'yu doğrudan değiştirir. Kopya olmazsa "Değişiklikleri At"
    /// kayıttaki değeri de değiştirir. Kopyayı Framework'ün JsonDeepCopyService sınıfı üretir. Liste alanlarını da kopyalar.
    /// Her depo bir ağaç olarak saklanır: düğümün DTO'su ve alt düğümleri (StoredNode).
    /// </remarks>
    public class WarehouseStore : IHierarchyLoader, IHierarchyPersister
    {
        private readonly JsonDeepCopyService copier = new();
        private readonly Dictionary<long, StoredNode> warehouses = new();

        /// <summary>
        /// Örnek veriyle kayıt katmanını kurar.
        /// </summary>
        public WarehouseStore()
        {
            Add(CreateFirstWarehouse());
            Add(CreateSecondWarehouse());
        }

        /// <summary>
        /// Kayıt sayısı. Testler kaydın çalıştığını bununla denetler.
        /// </summary>
        public int SaveCount { get; private set; }

        /// <summary>
        /// Sol paneldeki depolar.
        /// </summary>
        public IEnumerable<HierarchyRootInfo> Roots => warehouses.Values.Select(ToRootInfo);

        /// <summary>
        /// Saklanan deponun kopyası değil, kendisi. Yalnızca testler kullanır: kayıttan sonra saklanan değeri okur.
        /// </summary>
        /// <param name="id">Depo kimliği</param>
        /// <returns>Saklanan depo ağacı</returns>
        public StoredNode GetStored(long id) => warehouses[id];

        /// <summary>
        /// Yeni bir depo ekler. Testler büyük veriyle depo kurmak için kullanır.
        /// </summary>
        /// <param name="warehouse">Depo ağacı. Kök DTO'su WarehouseDto olmalı</param>
        public void Add(StoredNode warehouse) => warehouses[((WarehouseDto)warehouse.Dto).Id] = warehouse;

        /// <inheritdoc />
        public Task<Hierarchy> LoadAsync(HierarchySchema schema, object rootId, CancellationToken cancellationToken)
        {
            // rootId: Roots içindeki HierarchyRootInfo.Id değeridir. Bu örnekte long'dur.
            if (!warehouses.TryGetValue((long)rootId, out var stored))
            {
                throw new InvalidOperationException($"{rootId} kimlikli depo yok. Roots listesindeki kimlikleri denetle.");
            }

            var hierarchy = new Hierarchy(schema, Copy(stored.Dto));
            AttachChildren(hierarchy, hierarchy.Root, stored);
            return Task.FromResult(hierarchy);
        }

        /// <inheritdoc />
        public Task SaveAsync(Hierarchy hierarchy, HierarchyChangeSet changes, CancellationToken cancellationToken)
        {
            // Bellekte tüm ağaç yeniden yazılır. Gerçek proje changes.Added, Modified, Deleted ve Reordered listelerini tek işlemde yazar.
            var warehouse = hierarchy.Root.GetDto<WarehouseDto>();
            warehouses[warehouse.Id] = Store(hierarchy.Root);
            SaveCount++;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Ağaç düğümü kurar. Örnek veri ve testler kullanır.
        /// </summary>
        /// <param name="dto">Düğümün DTO'su</param>
        /// <param name="children">Alt düğümler</param>
        /// <returns>Saklanan düğüm</returns>
        public static StoredNode Node(object dto, params StoredNode[] children) => new(dto, children);

        /// <summary>
        /// Stok hareketlerinden ürün kurar. Miktarı hareketlerden hesaplar.
        /// </summary>
        /// <param name="code">Ürün kodu</param>
        /// <param name="name">Ürün adı</param>
        /// <param name="unitWeight">Birim ağırlık (kg)</param>
        /// <param name="movements">Stok hareketleri</param>
        /// <returns>Ürün</returns>
        public static ProductDto Product(string code, string name, double? unitWeight, params StockMovementDto[] movements)
        {
            var product = new ProductDto { Code = code, Name = name, UnitWeight = unitWeight, StockMovements = movements.ToList() };
            for (var i = 0; i < product.StockMovements.Count; i++)
            {
                product.StockMovements[i].Order = i;
            }

            product.Quantity = StockCalculator.CalculateQuantity(product.StockMovements);
            return product;
        }

        /// <summary>
        /// Stok hareketi kurar.
        /// </summary>
        /// <param name="date">Tarih, "yyyy-MM-dd" biçiminde (ör. "2026-11-10")</param>
        /// <param name="type">Giriş ya da çıkış</param>
        /// <param name="quantity">Miktar</param>
        /// <param name="note">Açıklama</param>
        /// <returns>Stok hareketi</returns>
        public static StockMovementDto Movement(string date, MovementType type, int? quantity, string note = "")
        {
            var day = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new StockMovementDto { Date = day, Type = type, Quantity = quantity, Note = note };
        }

        // Örnek veri. "Ankara Ana Depo" hatasızdır. "İzmir Depo" deposunda bir hata ve uyarılar vardır: form açılınca görünür.
        private static StoredNode CreateFirstWarehouse()
        {
            return Node(
                new WarehouseDto { Id = 1, Name = "Ankara Ana Depo", Code = "D-01", City = "Ankara" },
                Node(
                    new ShelfDto { Code = "R-01", MaxLoad = 500 },
                    Node(Product("U-001", "Vida kutusu", 2.5, Movement("2026-11-01", MovementType.In, 100, "İlk giriş"), Movement("2026-11-03", MovementType.Out, 20, "Atölye"))),
                    Node(Product("U-002", "Somun kutusu", 1.5, Movement("2026-11-01", MovementType.In, 50, "İlk giriş")))),
                Node(
                    new ShelfDto { Code = "R-02", MaxLoad = 1000 },
                    Node(Product("U-003", "Matkap", 3, Movement("2026-11-02", MovementType.In, 10, "İlk giriş")))));
        }

        private static StoredNode CreateSecondWarehouse()
        {
            // Şehir boş: uyarı. Ürünün kodu boş: hata. Çıkışın açıklaması boş: uyarı.
            return Node(
                new WarehouseDto { Id = 2, Name = "İzmir Depo", Code = "D-02", City = string.Empty },
                Node(
                    new ShelfDto { Code = "R-01", MaxLoad = 300 },
                    Node(Product(string.Empty, "Çimento torbası", 50, Movement("2026-11-01", MovementType.In, 5, "İlk giriş"), Movement("2026-11-04", MovementType.Out, 1)))));
        }

        private static HierarchyRootInfo ToRootInfo(StoredNode node)
        {
            var warehouse = (WarehouseDto)node.Dto;
            return new HierarchyRootInfo(warehouse.Id, warehouse.Name, warehouse.Code, $"{node.Children.Length} raf");
        }

        private void AttachChildren(Hierarchy hierarchy, HierarchyNode parent, StoredNode stored)
        {
            foreach (var child in stored.Children)
            {
                AttachChildren(hierarchy, hierarchy.Attach(parent, Copy(child.Dto)), child);
            }
        }

        private StoredNode Store(HierarchyNode node) => new(Copy(node.Dto), node.Children.Select(Store).ToArray());

        private object Copy(object dto)
        {
            return copier.DeepCopy(dto) ?? throw new InvalidOperationException($"{dto.GetType().Name} kopyalanamadı. Sınıfın JSON'a yazılabildiğini denetle.");
        }
    }

    /// <summary>
    /// Saklanan ağaç düğümü: DTO ve alt düğümleri.
    /// </summary>
    /// <param name="Dto">Düğümün DTO'su</param>
    /// <param name="Children">Alt düğümler</param>
    public sealed record StoredNode(object Dto, StoredNode[] Children);
}
