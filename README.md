# Depo envanteri (GVD Framework örneği)

Bu proje GVD Framework'ü kullanan bir depo envanteri uygulamasıdır. Framework'ün kaynak kodu `reddeadair/gvdframeworka`
deposundadır. Bu proje Framework'ü yalnızca NuGet paketinden alır.

Ağaç: Depo → Raf (bir depoda en çok 50) → Ürün. Üründe stok hareketi tablosu vardır.

## Projeler

| Proje | Hedef | İçerik |
|---|---|---|
| `Gvd.Framework.Depo.Core` | `net8.0` | Veri, kurallar, şema, kayıt katmanı ve düğüm ViewModel'leri. WPF yok |
| `Gvd.Framework.Depo.Tests` | `net8.0` | Pencere açmayan testler. Oturum, kurallar, tablo ve 10.000 satır hız testi |
| `Gvd.Framework.Depo.App` | `net8.0-windows` | Pencere, formlar ve etiketler. Yalnızca Windows'ta derlenir |

## Kurulum

Paketler `_packages` klasöründen gelir.

- `_packages/Gvd.Framework.1.0.0.nupkg` depoda vardır. Core ve Tests bu paketle derlenir.
- App için `Gvd.Framework.Wpf` paketi de gerekir. Bu paketi Windows'ta üret:
  1. `gvdframeworka` deposunda `Scripts/Pack-All.ps1` çalıştır.
  2. O deponun `_packages` klasöründeki `Gvd.Framework.*.nupkg` dosyalarını bu deponun `_packages` klasörüne kopyala.
  3. Bu deponun `packages` klasörünü sil. NuGet aynı sürüm numaralı paketi önbellekten okur.

## Komutlar

Testleri çalıştır:

```text
cd Gvd.Framework.Depo.Tests
dotnet test
```

Pencereyi aç (Windows):

```text
cd Gvd.Framework.Depo.App
dotnet run
```

İnternetsiz bilgisayarda bağımlılıkları `_offline/feed` klasörüne koy. Sonra `NuGet.Offline.config` ile restore et.
Ayrıntı: `NuGet.Offline.config` dosyasının yorumu.

## Kodu okuma sırası

1. `Gvd.Framework.Depo.Core/DepoDtos.cs`: veri. Yeni alan ekleme adımları sınıfın yorumundadır.
2. `DepoValidators.cs` ve `ProductHierarchyRules.cs`: kurallar.
3. `DepoSetup.cs`: şema, kurallar, ViewModel kaydı ve workspace.
4. `DepoViewModels.cs`: formların ViewModel'leri.
5. `WarehouseStore.cs`: bellek içi kayıt katmanı. Gerçek proje veritabanı kullanır.
6. `Gvd.Framework.Depo.App/App.xaml.cs` ve `MainWindow.xaml`: uygulama ve pencere.

Framework rehberi: `gvdframeworka` deposunda `Framework/README.md`.
