import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';

class StartupPage extends StatelessWidget {
  const StartupPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor:
          Color.fromARGB(255, 255, 255, 255), // Adjusted opacity to full
      body: Center(
        // Center widget to center its child
        child: Text(
          "Great",
          style: TextStyle(fontSize: 24), // Example style
        ),
      ),
    );
  }
}
