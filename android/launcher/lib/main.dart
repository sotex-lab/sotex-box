import 'dart:isolate';
import 'dart:ui';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_splash_screen/flutter_splash_screen.dart';
import 'package:launcher/app_observer.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/app_router_delegate.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:launcher/src/sse/sse_entry.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  RootIsolateToken rootToken = RootIsolateToken.instance!;
  ReceivePort receivePort = ReceivePort();
  Isolate.spawn(sseEntryPoint, receivePort.sendPort);
  SendPort sendPort = await waitForSendPort(receivePort);
  sendPort.send(rootToken);

  Bloc.observer = const AppObserver();
  runApp(const SotexBox());
}

class SotexBox extends StatefulWidget {
  const SotexBox({super.key});

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
    return MaterialApp(
        home: MultiBlocProvider(
            providers: [
          BlocProvider(create: (context) => NavigationCubit()),
          BlocProvider(create: (context) => NetworkCubit()),
        ],
            child: MaterialApp.router(
              theme: ThemeData(
                  brightness: Brightness.dark, primaryColor: Colors.blueGrey),
              darkTheme: ThemeData(
                  brightness: Brightness.dark, primaryColor: Colors.blueGrey),
              themeMode: ThemeMode.system,
              routerDelegate: AppRouterDelegate(),
            )));
  }
}
