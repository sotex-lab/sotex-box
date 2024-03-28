package com.example.launcher

import io.flutter.embedding.android.FlutterActivity
import android.os.Bundle
import io.flutter.embedding.android.FlutterActivityLaunchConfigs.BackgroundMode.transparent
import org.devio.flutter.splashscreen.SplashScreen

class MainActivity: FlutterActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        SplashScreen.show(this, true)
        // intent.putExtra("background_mode", transparent.toString())
        super.onCreate(savedInstanceState)
    }
}
