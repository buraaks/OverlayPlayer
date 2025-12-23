# OverlayPlayer Publish Script
# Bu script uygulamayı tek bir .exe dosyası haline getirir.

$projectPath = "./OverlayPlayer/OverlayPlayer.csproj"
$outputPath = "./Publish"

Write-Host "Yayınlama işlemi başlatılıyor..." -ForegroundColor Cyan

# Varsa eski publish klasörünü temizle
if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Recurse -Force
}

# Dotnet publish komutu
# -c Release: Release modunda derle
# -r win-x64: Windows 64-bit hedefle
# --self-contained true: .NET runtime'ı içine göm (Kullanıcının .NET kurmasına gerek kalmaz)
# -p:PublishSingleFile=true: Her şeyi tek bir EXE içine paketle
# -p:PublishReadyToRun=true: Başlangıç performansını artırmak için ön derleme yap
# -p:IncludeNativeLibrariesForSelfExtract=true: Gerekli kütüphaneleri EXE içine dahil et

dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -o $outputPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBaşarılı! Uygulama '$outputPath' klasörüne çıkarıldı." -ForegroundColor Green
    Write-Host "Dağıtıma hazır dosya: OverlayPlayer.exe" -ForegroundColor Yellow
} else {
    Write-Host "`nHata: Yayınlama işlemi başarısız oldu." -ForegroundColor Red
}
