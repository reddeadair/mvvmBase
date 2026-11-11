using Gvd.Framework.Hierarchy;
using Gvd.Framework.Validation;

namespace DepoSample.Tests
{
    /// <summary>
    /// Düğümler arası kural (ürün ağırlığı ≤ raf yükü) ve raf sayısı sınırı testleri.
    /// </summary>
    public class DepoRuleTests
    {
        /// <summary>
        /// Ürünün birim ağırlığı artınca toplam ağırlık raf yükünü aşar. Ürünün miktarı ve rafın yükü birlikte kırmızı olur.
        /// Raf yükü artınca iki hata da kalkar.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task ProductWeight_OverShelfLoad_MarksBothNodes()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var shelf = context.Shelf(0);
            var product = context.Product(0, 0);
            Assert.Equal(80, product.Quantity.Value);

            // 80 adet × 10 kg = 800 kg. Raf en çok 500 kg taşır.
            product.UnitWeight.Value = 10;

            Assert.Equal(MessageLevel.Error, product.Quantity.MessageLevel);
            Assert.Equal(MessageLevel.Error, shelf.MaxLoad.MessageLevel);
            Assert.Contains("800 kg", shelf.MaxLoad.Messages.Message);
            Assert.Equal(2, context.Session.ErrorCount);

            shelf.MaxLoad.Value = 1000;

            Assert.Equal(MessageLevel.None, product.Quantity.MessageLevel);
            Assert.Equal(MessageLevel.None, shelf.MaxLoad.MessageLevel);
            Assert.Equal(0, context.Session.ErrorCount);
        }

        /// <summary>
        /// Tabloya giriş hareketi eklenince miktar yeniden hesaplanır. Düğümler arası kural yeni miktarla çalışır.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task AddingStockMovement_RecalculatesQuantity_AndRunsCrossNodeRule()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var shelf = context.Shelf(0);
            var product = context.Product(0, 0);

            // 80 + 120 = 200 adet × 2,5 kg = 500 kg. Sınırdadır, hata yoktur.
            product.StockMovements.AddNew(WarehouseStore.Movement("2026-11-05", MovementType.In, 120, "Ek giriş"));
            Assert.Equal(200, product.Quantity.Value);
            Assert.Equal(200, product.Dto.Quantity);
            Assert.Equal(MessageLevel.None, shelf.MaxLoad.MessageLevel);

            // Satırın miktarı 121 olunca 201 × 2,5 = 502,5 kg. Sınır aşılır.
            product.StockMovements.Items[2].Quantity.Value = 121;

            Assert.Equal(201, product.Quantity.Value);
            Assert.Equal(MessageLevel.Error, product.Quantity.MessageLevel);
            Assert.Equal(MessageLevel.Error, shelf.MaxLoad.MessageLevel);

            // Satır silinince miktar 80 olur. Hata kalkar.
            product.StockMovements.RemoveAt(2);

            Assert.Equal(80, product.Quantity.Value);
            Assert.Equal(MessageLevel.None, shelf.MaxLoad.MessageLevel);
            Assert.Equal(0, context.Session.ErrorCount);
        }

        /// <summary>
        /// Çıkış girişten büyük olunca miktar eksiye düşer. Ürünün miktar kuralı hata verir.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task NegativeStock_IsError()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var product = context.Product(0, 1);

            product.StockMovements.AddNew(WarehouseStore.Movement("2026-11-06", MovementType.Out, 60, "Fazla çıkış"));

            Assert.Equal(-10, product.Quantity.Value);
            Assert.Equal(MessageLevel.Error, product.Quantity.MessageLevel);
            Assert.Equal("Miktar eksiye düştü. Çıkış hareketlerini denetle.", product.Quantity.Messages.Message);
        }

        /// <summary>
        /// Depoda 50 raf varken yeni raf eklenemez. Ekle komutu kapalıdır, Add hata verir. Bir raf silinince ekleme açılır.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task ShelfLimit_FiftyShelves()
        {
            var store = new WarehouseStore();
            store.Add(CreateWarehouseWithShelves(10, DepoSetup.MaxShelvesPerWarehouse));
            using var context = await DepoTestContext.OpenAsync(10L, store);
            var hierarchy = context.Session.Hierarchy;

            Assert.False(hierarchy.CanAdd(hierarchy.Root, typeof(ShelfDto), out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
            Assert.Throws<HierarchyException>(() => hierarchy.Add(hierarchy.Root, new ShelfDto { Code = "R-51", MaxLoad = 100 }));

            hierarchy.Remove(hierarchy.Root.Children[0]);

            Assert.True(hierarchy.CanAdd(hierarchy.Root, typeof(ShelfDto), out _));
        }

        /// <summary>
        /// Workspace'te 50 raflı depoda Ekle komutu kapalıdır.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task ShelfLimit_WorkspaceAddCommandDisabled()
        {
            var store = new WarehouseStore();
            store.Add(CreateWarehouseWithShelves(10, DepoSetup.MaxShelvesPerWarehouse));
            var workspace = DepoSetup.CreateWorkspace(store, new AlwaysDiscardPrompt());
            var root = workspace.Roots.Single(item => item.Name == "Büyük Depo");

            await workspace.SelectCommand.ExecuteAsync(root);
            var shelfType = Assert.Single(workspace.AddableTypes);

            Assert.False(workspace.AddCommand.CanExecute(shelfType));
        }

        private static StoredNode CreateWarehouseWithShelves(long id, int shelfCount)
        {
            var shelves = Enumerable.Range(1, shelfCount)
                .Select(index => WarehouseStore.Node(new ShelfDto { Code = $"R-{index:00}", MaxLoad = 100 }))
                .ToArray();
            return WarehouseStore.Node(new WarehouseDto { Id = id, Name = "Büyük Depo", Code = "D-10", City = "Bursa" }, shelves);
        }
    }

    /// <summary>
    /// Kaydedilmemiş değişiklik sorusunu hep "kaydetme" diye yanıtlar. Yalnızca testler kullanır.
    /// </summary>
    public class AlwaysDiscardPrompt : ISaveChangesPrompt
    {
        /// <inheritdoc />
        public Task<SaveChangesChoice> AskAsync(string rootName, bool canSave) => Task.FromResult(SaveChangesChoice.Discard);
    }
}
