import 'package:flutter/material.dart';
import 'package:flutter_splash_screen/flutter_splash_screen.dart';
import 'package:launcher/safe_casting.dart';

void main() {
  runApp(const SotexBox());
}

class SotexBox extends StatefulWidget {
  const SotexBox({super.key});

  @override
  State<StatefulWidget> createState() => SotexBoxState();
}

class SotexBoxState extends State<SotexBox> {
  int get splashScreenDuration => tryCastInt(
      const String.fromEnvironment("splash_screen_duration", defaultValue: ""),
      fallback: 3600);
  @override
  void initState() {
    super.initState();
    hideScreen();
  }

  Future<void> hideScreen() async {
    Future.delayed(Duration(milliseconds: splashScreenDuration), () {
      FlutterSplashScreen.hide();
    });
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      home: Scaffold(
        appBar: AppBar(
          title: const Text('Sotex Box'),
        ),
        body: const Center(
          child: Text(
            'Sotex Solutions',
            style: TextStyle(fontSize: 20),
          ),
        ),
      ),
    );
  }
}
