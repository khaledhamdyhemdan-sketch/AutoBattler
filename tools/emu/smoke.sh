#!/usr/bin/env bash
# Emulator smoke check: install APK, launch, capture screenshots + logcat.
set -u
OUT=emu-out; mkdir -p $OUT
APK=apk/Chronicles_Android.apk
BT=$(ls -d $ANDROID_HOME/build-tools/* | sort -V | tail -1)
$BT/aapt dump badging $APK 2>&1 | grep -E "package:|native-code|launchable|sdkVersion" > $OUT/badging.txt
$BT/apksigner verify --print-certs $APK > $OUT/signer.txt 2>&1
PKG=$(sed -n "s/.*package: name='\([^']*\)'.*/\1/p" $OUT/badging.txt | head -1)
echo "pkg=$PKG" > $OUT/summary.txt
adb shell getprop ro.product.cpu.abilist >> $OUT/summary.txt
adb shell wm size >> $OUT/summary.txt
adb install -r $APK > $OUT/install.txt 2>&1; cat $OUT/install.txt >> $OUT/summary.txt
adb logcat -c
adb shell settings put secure immersive_mode_confirmations confirmed
adb shell wm size 540x960 >/dev/null 2>&1; adb shell wm density 240 >/dev/null 2>&1
adb shell monkey -p "$PKG" -c android.intent.category.LAUNCHER 1 >> $OUT/summary.txt 2>&1
sleep 25
shot(){ adb exec-out screencap -p > $OUT/$1.png; adb shell pidof "$PKG" >/dev/null 2>&1 || echo "process gone at $1" >> $OUT/summary.txt; }
tapw(){ adb shell input tap $1 $2; sleep 4; }
shot a_home_guide
tapw 222 247; tapw 222 247; tapw 222 247; tapw 222 247; tapw 222 247
shot b_home_after_skip
tapw 299 843; sleep 3; shot c_summon
tapw 128 843; sleep 3; shot d_heroes
tapw 226 843; sleep 3; shot e_story
tapw 393 843; sleep 3; shot f_events
tapw 477 843; sleep 3; shot g_more
tapw 42 843; sleep 3; shot h_home
adb logcat -d > $OUT/logcat.txt 2>&1
grep -iE "FATAL|Unity|libil2cpp|Vulkan|GLES|native-bridge|ndk_translation|houdini" $OUT/logcat.txt | head -80 > $OUT/logcat_key.txt
exit 0
