# 🎬 OverlayPlayer

OverlayPlayer, masaüstünüzün en önünde (Topmost), şeffaf ve tıklamayı engelleyen (Click-through) bir medya oynatıcıdır. GIF ve Video dosyalarını destekler. Özellikle yayıncılar, ekranında sürekli hareketli bir şeyler görmek isteyenler veya masaüstüne şık bir animasyon eklemek isteyenler için tasarlanmıştır.

## ✨ Özellikler

-   **Her Zaman Üstte:** Diğer pencerelerin üzerinde kalır.
-   **Şeffaflık & Borderless:** Çerçevesizdir ve arka planı tamamen şeffaftır.
-   **Tıklama Geçirme (Click-through):** Animasyonun altındaki pencerelere tıklayabilirsiniz; sanki orada değilmiş gibi davranır.
-   **Otomatik Yerleşim:** Başladığında otomatik olarak ekranın sol alt köşesine yerleşir.
-   **Geniş Format Desteği:** `.gif`, `.png`, `.jpg`, `.jpeg`, `.bmp`, `.mp4`, `.avi`, `.mov`, `.wmv` dosyalarını destekler.
-   **Sistem Tepsisi (Tray) Kontrolü:** Uygulamayı sistem tepsisinden yönetebilir, medyanızı değiştirebilir veya durdurabilirsiniz.

## 🚀 Kurulum & Çalıştırma

### Hazır Sürümü Kullanma
1.  [Releases](https://github.com/buraaks/OverlayPlayer/releases) kısmından en güncel `OverlayPlayer.exe` dosyasını indirin.
2.  Doğrudan çalıştırın.
3.  Dosya seç ekranından bir GIF veya Video seçin.

### Kaynak Koddan Derleme
Projeyi kendiniz derlemek isterseniz:
1.  Depoyu klonlayın: `git clone https://github.com/buraaks/OverlayPlayer.git`
2.  `.NET 8 SDK` yüklü olduğundan emin olun.
3.  Proje klasöründe terminali açın ve `powershell ./publish.ps1` komutunu çalıştırın.
4.  `Publish` klasörü içinde tek dosyalık `.exe` dosyanız hazır olacak.

## 🛠️ Kullanılan Teknolojiler
-   **C# / WPF** (.NET 8)
-   **WPF-Animated-Gif** (GIF oynatma desteği için)
-   **Windows API (User32.dll)** (Click-through ve pencere yönetimi için)

## 📝 Lisans
Bu proje MIT lisansı ile lisanslanmıştır. İstediğiniz gibi kullanabilir ve geliştirebilirsiniz.

---
*Geliştiren: [Burak](https://github.com/buraaks)*
