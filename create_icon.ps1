# Logo.ico Oluşturma Script'i
# PNG dosyasını ICO formatına dönüştürür

$pngPath = ".\OverlayPlayer\logo.png"
$icoPath = ".\OverlayPlayer\logo.ico"

if (-not (Test-Path $pngPath)) {
    Write-Host "Hata: logo.png bulunamadı!" -ForegroundColor Red
    exit 1
}

Write-Host "ICO dosyası oluşturuluyor..." -ForegroundColor Cyan

# .NET kullanarak PNG'yi ICO'ya dönüştür
Add-Type -AssemblyName System.Drawing

try {
    # PNG'yi yükle
    $bitmap = New-Object System.Drawing.Bitmap($pngPath)
    
    # ICO dosyası için stream oluştur
    $icoStream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
    
    # Icon oluştur ve kaydet
    $icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
    $icon.Save($icoStream)
    
    $icoStream.Close()
    $icon.Dispose()
    $bitmap.Dispose()
    
    Write-Host "✓ logo.ico başarıyla oluşturuldu!" -ForegroundColor Green
    Write-Host "Dosya: $icoPath" -ForegroundColor Yellow
}
catch {
    Write-Host "Hata: ICO oluşturulamadı: $_" -ForegroundColor Red
    Write-Host "`nAlternatif: Online bir PNG to ICO converter kullanabilirsiniz:" -ForegroundColor Yellow
    Write-Host "https://convertio.co/png-ico/" -ForegroundColor Cyan
    Write-Host "https://www.icoconverter.com/" -ForegroundColor Cyan
    exit 1
}

