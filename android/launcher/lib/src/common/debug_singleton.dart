import 'package:launcher/src/common/logging.dart';

class DebugSingleton {
  // Private constructor
  DebugSingleton._privateConstructor();

  static final DebugSingleton _instance = DebugSingleton._privateConstructor();

  factory DebugSingleton() {
    return _instance;
  }

  final DebugBloc bloc = DebugBloc();

  DebugBloc get getDebugBloc => bloc;
}
