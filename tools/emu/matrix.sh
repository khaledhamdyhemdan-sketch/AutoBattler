#!/usr/bin/env bash
# Launch-stability matrix: each APK, 3 launches, 35s each. Reports survive/crash + SIGILL.
set -u
OUT=emu-out; mkdir -p $OUT; : > $OUT/matrix.txt
PKG=com.mythicchronicles.alpha
adb shell settings put secure immersive_mode_confirmations confirmed
for f in apk/*.apk; do
  adb uninstall $PKG >/dev/null 2>&1
  adb install -r "$f" >/dev/null 2>&1 || { echo "$f install failed" >> $OUT/matrix.txt; continue; }
  for n in 1 2 3; do
    adb shell am force-stop $PKG; adb logcat -c
    adb shell monkey -p $PKG -c android.intent.category.LAUNCHER 1 >/dev/null 2>&1
    sleep 35
    if adb shell pidof $PKG >/dev/null 2>&1; then r=alive; else r=DEAD; fi
    s=$(adb logcat -d | grep -c "Fatal signal 4")
    echo "$f launch$n $r sigill=$s" >> $OUT/matrix.txt
    adb logcat -d | grep -E "Fatal signal" | head -2 >> $OUT/matrix.txt
  done
done
exit 0
