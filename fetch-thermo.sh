#!/usr/bin/env bash
# Downloads Thermo Fisher RawFileReader (.NET 8) from the official repository
# https://github.com/thermofisherlsms/RawFileReader and extracts the DLLs into ./lib/.
# The RawFileReader license (lib/THERMO_LICENSE.doc after download) applies to these files;
# they are NOT part of this project's open-source license and must not be committed to the repository.
set -eu
cd "$(dirname "$0")"
VER=${THERMO_VERSION:-8.0.42}
BASE=https://raw.githubusercontent.com/thermofisherlsms/RawFileReader/main/Libs/NetCore/Net8
TMP=$(mktemp -d)
mkdir -p lib
for pkg in thermofisher.commoncore.data thermofisher.commoncore.rawfilereader; do
  curl -fsSL -o "$TMP/$pkg.zip" "$BASE/$pkg.$VER.nupkg"
  unzip -qo "$TMP/$pkg.zip" 'lib/net8.0/*.dll' -d "$TMP/$pkg"
  cp "$TMP/$pkg"/lib/net8.0/*.dll lib/
done
curl -fsSL -o "$TMP/License.doc" https://raw.githubusercontent.com/thermofisherlsms/RawFileReader/main/License.doc && cp "$TMP/License.doc" lib/THERMO_LICENSE.doc || true
rm -rf "$TMP"
ls -l lib
echo "RawFileReader $VER extracted to ./lib"
