#!/bin/bash
# Spustí GC-MS porovnanie (macOS). Odstráni karanténu, ktorú macOS pridáva stiahnutým súborom.
cd "$(dirname "$0")"
xattr -dr com.apple.quarantine . 2>/dev/null
chmod +x ./GcmsPorovnanie
exec ./GcmsPorovnanie
