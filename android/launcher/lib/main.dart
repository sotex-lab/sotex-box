import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_splash_screen/flutter_splash_screen.dart';
import 'package:launcher/app_observer.dart';
import 'package:launcher/src/channels/bloc/playback_bloc.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/database/database.dart';
import 'package:launcher/src/navigation/app_router_delegate.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:launcher/src/sse/processing/schedule_processor.dart';
import 'package:launcher/src/sse/sse_entry.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final db = BoxDatabase();
  //await db.clean();
  LogManager().removeLogFile();
  startListeningForSSE();
  ScheduleProcessor(SSEScheduleMessage()).process();
  Bloc.observer = const AppObserver();
  runApp(const SotexBox());
}

void spammer() async {
  while (true) {
    await Future.delayed(const Duration(seconds: 3));
    (await LogManager().getOrCreateLogger()).i("Hello World");
  }
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
          BlocProvider(create: (context) => PlaybackBloc()),
          BlocProvider(create: (context) => LogBloc())
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
