import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_splash_screen/flutter_splash_screen.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:launcher/src/navigation/sotex_box_information_parser.dart';
import 'package:launcher/src/navigation/sotex_box_router_delegate.dart';

void main() {
  runApp(SotexBox());
}

class SotexBox extends StatefulWidget {
  SotexBox({super.key});

  @override
  State<StatefulWidget> createState() => SotexBoxState();
}

class SotexBoxState extends State<SotexBox> {
  final navigationCubit = NavigationCubit();
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
    return MaterialApp.router(
      routerDelegate: SotexBoxRouterDelegate(navigationCubit),
      routeInformationParser: SotexBoxInformationParser(),
      theme:
          ThemeData(brightness: Brightness.dark, primaryColor: Colors.blueGrey),
      darkTheme:
          ThemeData(brightness: Brightness.dark, primaryColor: Colors.blueGrey),
      themeMode: ThemeMode.system,
    );
  }
}

    // return BlocProvider<NetworkCubit>(
    //   create: (context) => NetworkCubit(),
    //   child: BlocListener<NetworkCubit, NetworkState>(
    //     listener: (context, state) {
    //       if (state == NetworkState.offline) {
    //         Navigator.of(context).pushAndRemoveUntil(
    //           MaterialPageRoute(builder: (context) => const WifiPickerPage()),
    //           (Route<dynamic> route) => false,
    //         );
    //       }
    //     },
    //     child: MaterialApp(
    //       home: const WifiPickerPage(),
    //     ),
    //   ),
    // );