import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_splash_screen/flutter_splash_screen.dart';
import 'package:launcher/app_observer.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/app_navigator.dart';
import 'package:launcher/src/navigation/app_router_delegate.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';

void main() {
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
