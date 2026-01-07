; OverlayPlayer Inno Setup Script
; Bu script, yayınlanmış olan .exe dosyasını profesyonel bir kurulum sihirbazına dönüştürür.

[Setup]
AppId={{D3B3A5E1-C8A4-4B9E-B3D9-9F8A7E6D5C4B}
AppName=Overlay Player
AppVersion=2.2.0
AppPublisher=Burak
AppPublisherURL=https://github.com/buraaks/OverlayPlayer
AppSupportURL=https://github.com/buraaks/OverlayPlayer
AppUpdatesURL=https://github.com/buraaks/OverlayPlayer
DefaultDirName={autopf}\Overlay Player
DefaultGroupName=Overlay Player
AllowNoIcons=yes
; Kurulum dosyasının çıkış klasörü
OutputDir=.\SetupOutput
OutputBaseFilename=OverlayPlayer_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\OverlayPlayer.exe
; LicenseFile= (opsiyonel: lisans dosyası eklenebilir)
; InfoBeforeFile= (opsiyonel: kurulum öncesi bilgi dosyası)
; InfoAfterFile= (opsiyonel: kurulum sonrası bilgi dosyası)
SetupIconFile=.\OverlayPlayer\logo.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Registry]
Root: HKCU; Subkey: "Software\OverlayPlayer"; ValueType: string; ValueName: "Language"; ValueData: "{language}"; Flags: uninsdeletekey

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Yayınladığımız tek EXE dosyasını kaynak olarak alıyoruz
Source: ".\Publish\OverlayPlayer.exe"; DestDir: "{app}"; Flags: ignoreversion
; Eğer uygulama ile beraber gelmesi gereken başka klasörler varsa buraya eklenebilir

[Icons]
Name: "{group}\Overlay Player"; Filename: "{app}\OverlayPlayer.exe"
Name: "{commondesktop}\Overlay Player"; Filename: "{app}\OverlayPlayer.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\OverlayPlayer.exe"; Description: "{cm:LaunchProgram,Overlay Player}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Cache"
Type: filesandordirs; Name: "{localappdata}\OverlayPlayer"
