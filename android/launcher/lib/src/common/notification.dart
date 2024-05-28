import 'package:launcher/src/common/logging.dart';

class Notification {
  Notification._privateConstructor();

  static final Notification _instance = Notification._privateConstructor();

  factory Notification() {
    LogManager().ensureCreated();
    return _instance;
  }

  void i(String message) async {
    // DebugSingleton().getDebugBloc.add(DebugPushEvent(message));
    LogManager().i(message);
  }

  void e(String message) async {
    // DebugSingleton().getDebugBloc.add(DebugPushEvent(message));
    LogManager().e(message);
  }

  void w(String message) async {
    // DebugSingleton().getDebugBloc.add(DebugPushEvent(message));
    LogManager().w(message);
  }
}
