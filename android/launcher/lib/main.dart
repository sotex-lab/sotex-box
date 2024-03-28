import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_splash_screen/flutter_splash_screen.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/common/network/cubits/network_cubit.dart';
import 'package:launcher/src/common/network/network.dart';

void main() {
  runApp(const SotexBox());
}

class SotexBox extends StatefulWidget {
  const SotexBox({super.key});

  @override
  State<StatefulWidget> createState() => SotexBoxState();
}

class SotexBoxState extends State<SotexBox> {
  @override
  void initState() {
    super.initState();
    hideScreen();
  }

  Future<void> hideScreen() async {
    Future.delayed(const Duration(milliseconds: 3600), () {
      FlutterSplashScreen.hide();
    });
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider<NetworkCubit>(
      create: (context) => NetworkCubit(),
      child: BlocListener<NetworkCubit, NetworkState>(
        listener: (context, state) {
          if (state == NetworkState.offline) {
            Navigator.of(context).pushAndRemoveUntil(
              MaterialPageRoute(builder: (context) => const WifiPickerPage()),
              (Route<dynamic> route) => false,
            );
          }
        },
        child: const MaterialApp(
          home: WifiPickerPage(),
        ),
      ),
    );
  }
}
