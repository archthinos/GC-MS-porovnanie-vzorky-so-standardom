#!/usr/bin/env bash
# Builds self-contained single-file executables for all platforms and packs them as release zips.
#   bash build.sh            -> vystup/<rid>/ and vystup/GcmsPorovnanie-<version>-<rid>.zip (+ SHA256SUMS.txt)
# Requires .NET 8 SDK (downloaded to /tmp/dotnet-sdk if missing). On macOS hosts the Mac binaries are
# ad-hoc signed with codesign; elsewhere with rcodesign if it is on PATH.
set -eu
cd "$(dirname "$0")"
DN=${DOTNET:-}
if [ -z "$DN" ]; then
  if command -v dotnet >/dev/null; then DN=dotnet
  else
    [ -x /tmp/dotnet-sdk/dotnet ] || { curl -sSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh; bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /tmp/dotnet-sdk; }
    DN=/tmp/dotnet-sdk/dotnet
  fi
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
# Thermo RawFileReader is not part of the repository – fetch it from the official source
[ -f lib/ThermoFisher.CommonCore.RawFileReader.dll ] || bash ./fetch-thermo.sh
VER=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' GcmsPorovnanie.csproj | head -1)
RIDS=${RIDS:-"win-x64 osx-arm64 osx-x64 linux-x64 linux-arm64"}
rm -rf vystup; mkdir -p vystup
for rid in $RIDS; do
  out=vystup/$rid
  "$DN" publish -c Release -r $rid --self-contained true -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "$out"
  rm -f "$out/web.config"
  if [[ $rid == osx-* ]]; then
    cp packaging/macos/Spustit.command "$out/"; chmod +x "$out/Spustit.command" "$out/GcmsPorovnanie"
    # Apple Silicon runs only signed binaries – ad-hoc signature
    if [[ $(uname) == Darwin ]]; then codesign --force -s - "$out/GcmsPorovnanie"
    elif command -v rcodesign >/dev/null; then rcodesign sign "$out/GcmsPorovnanie"
    else echo "WARNING: $rid not signed (install rcodesign or build on macOS)"; fi
  fi
  if [[ $rid == linux-* ]]; then chmod +x "$out/GcmsPorovnanie"; fi
  # zip without extra attributes (-X); folder inside the zip = GcmsPorovnanie-<ver>-<rid>
  name=GcmsPorovnanie-$VER-$rid
  (cd vystup && rm -rf "$name" && cp -rp "$rid" "$name" && zip -qrX "$name.zip" "$name" && rm -rf "$name")
done
(cd vystup && { sha256sum *.zip 2>/dev/null || shasum -a 256 *.zip; } > SHA256SUMS.txt)
echo "Done: $(pwd)/vystup"; ls -l vystup
