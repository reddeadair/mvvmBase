using Gvd.Framework.Validation;
using System.Diagnostics;
using Xunit.Abstractions;

namespace DepoSample.Tests
{
    /// <summary>
    /// 10.000 stok hareketli ürünün süre testi. Sınırlar geniştir (her adım 1 saniyeden kısa). Süreler test çıktısına yazılır.
    /// </summary>
    /// <remarks>
    /// Süreleri görmek için şu komutu çalıştır: dotnet test --logger "console;verbosity=detailed"
    /// </remarks>
    public class StockMovementPerformanceTests
    {
        private const int RowCount = 10000;
        private const long WarehouseId = 20;
        private static readonly TimeSpan Limit = TimeSpan.FromSeconds(1);

        private readonly ITestOutputHelper output;

        public StockMovementPerformanceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Açma, form kurma, doğrulama, hücre düzenleme ve satırların %10'unu silme adımlarını ölçer.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task TenThousandRows_StepsAreFast()
        {
            var store = new WarehouseStore();
            store.Add(CreateBigWarehouse());

            // Isınma: ilk çağrı JIT süresini de içerir. Ölçüm ikinci açılışla yapılır.
            using (var warmUp = await DepoTestContext.OpenAsync(WarehouseId, store))
            {
                warmUp.Product(0, 0);
            }

            var watch = Stopwatch.StartNew();
            using var context = await DepoTestContext.OpenAsync(WarehouseId, store);
            var open = Measure(watch, "Oturum açılışı (yükleme, kopya, tam doğrulama)");
            Assert.Equal(RowCount, context.Session.Hierarchy.Root.Children[0].Children[0].GetDto<ProductDto>().StockMovements.Count);

            watch.Restart();
            var product = context.Product(0, 0);
            var form = Measure(watch, "Ürün formu (10.000 satır ViewModel'i)");
            Assert.Equal(RowCount, product.StockMovements.Items.Count);

            watch.Restart();
            product.Validate();
            var validate = Measure(watch, "Formu yeniden doğrulama (Validate)");

            // Her 100. satırın açıklaması boş bir çıkıştır: 100 uyarı.
            Assert.Equal(RowCount / 100, context.WarningCount);
            Assert.Equal(0, context.Session.ErrorCount);

            watch.Restart();
            product.StockMovements.Items[5000].Quantity.Value = 0;
            var edit = Measure(watch, "Bir hücreyi değiştirme (hata çıkar)");
            Assert.Equal(1, context.Session.ErrorCount);

            // İkinci düzenleme: ilk düzenlemenin JIT süresi yoktur. Kullanıcının her tuşta beklediği süre budur.
            watch.Restart();
            product.StockMovements.Items[6000].Note.Value = "Düzeltildi";
            var secondEdit = Measure(watch, "İkinci hücre değişikliği (açıklama)");

            // Tablo dışındaki alan: ürün adı. Bu değişiklik tabloyu doğrulamamalı.
            watch.Restart();
            product.Name.Value = "Kablo bağı (siyah)";
            var nameEdit = Measure(watch, "Ürün adı değişikliği (tablo dışı alan)");

            watch.Restart();
            product.StockMovements.RemoveAll(row => row.Index % 10 == 0);
            var remove = Measure(watch, "Satırların %10'unu silme (1.000 satır)");
            Assert.Equal(RowCount - (RowCount / 10), product.StockMovements.Items.Count);
            Assert.Equal(product.StockMovements.Items.Count, product.Dto.StockMovements.Count);
            Assert.Equal(StockCalculator.CalculateQuantity(product.Dto.StockMovements), product.Quantity.Value);

            Assert.True(open < Limit, $"Açılış {open.TotalMilliseconds} ms sürdü.");
            Assert.True(form < Limit, $"Form {form.TotalMilliseconds} ms sürdü.");
            Assert.True(validate < Limit, $"Doğrulama {validate.TotalMilliseconds} ms sürdü.");
            Assert.True(edit < Limit, $"Hücre değişikliği {edit.TotalMilliseconds} ms sürdü.");
            Assert.True(secondEdit < Limit, $"İkinci hücre değişikliği {secondEdit.TotalMilliseconds} ms sürdü.");
            Assert.True(nameEdit < Limit, $"Ürün adı değişikliği {nameEdit.TotalMilliseconds} ms sürdü.");
            Assert.True(remove < Limit, $"Silme {remove.TotalMilliseconds} ms sürdü.");
        }

        private static StoredNode CreateBigWarehouse()
        {
            var movements = new StockMovementDto[RowCount];
            for (var i = 0; i < RowCount; i++)
            {
                // Her 100. satır açıklamasız çıkıştır. Diğerleri giriştir.
                movements[i] = i % 100 == 99
                    ? WarehouseStore.Movement("2026-11-01", MovementType.Out, 1)
                    : WarehouseStore.Movement("2026-11-01", MovementType.In, 2, "Giriş");
            }

            var product = WarehouseStore.Product("U-999", "Kablo bağı", 0.001, movements);
            return WarehouseStore.Node(
                new WarehouseDto { Id = WarehouseId, Name = "Büyük Hareketli Depo", Code = "D-20", City = "Konya" },
                WarehouseStore.Node(new ShelfDto { Code = "R-01", MaxLoad = 1000 }, WarehouseStore.Node(product)));
        }

        private TimeSpan Measure(Stopwatch watch, string step)
        {
            var elapsed = watch.Elapsed;
            output.WriteLine($"{step}: {elapsed.TotalMilliseconds:F1} ms");
            return elapsed;
        }
    }
}
