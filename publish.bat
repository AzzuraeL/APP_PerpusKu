@echo off
REM ============================================================
REM  PerpusKu - membuat aplikasi .exe mandiri (tanpa perlu .NET)
REM  Hasil: folder "publish" berisi PerpusKu.exe
REM ============================================================
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [GAGAL] .NET SDK tidak ditemukan. Instal Visual Studio dengan workload ".NET desktop development".
  pause
  exit /b 1
)

echo Memublikasikan PerpusKu (Release, win-x64, self-contained)...
dotnet publish "src\PerpusKu.App\PerpusKu.App.csproj" -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "publish"
if errorlevel 1 (
  echo.
  echo [GAGAL] Publish tidak berhasil. Lihat pesan galat di atas.
  pause
  exit /b 1
)

echo.
echo [SELESAI] Aplikasi ada di folder: %~dp0publish
echo Jalankan: publish\PerpusKu.exe
echo Catatan: folder "Database", "LatoFont", dan appsettings.json harus tetap berada di samping PerpusKu.exe.
pause
