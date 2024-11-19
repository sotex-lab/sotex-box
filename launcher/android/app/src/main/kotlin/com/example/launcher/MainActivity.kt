package com.sotex.box

import io.flutter.embedding.android.FlutterActivity
import android.os.Bundle
import io.flutter.embedding.android.FlutterActivityLaunchConfigs.BackgroundMode.transparent
import org.devio.flutter.splashscreen.SplashScreen

class MainActivity: FlutterActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        SplashScreen.show(this, true)
        super.onCreate(savedInstanceState)
    }
}
