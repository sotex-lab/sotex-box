import 'package:bloc/bloc.dart';
import 'package:launcher/logger.dart';

/// {%template counter_observer}
/// [BlocObserver] for the counter application which
/// observes all state changes
/// {%endtemplate}
class AppObserver extends BlocObserver {
  const AppObserver() : super();

  @override
  void onChange(BlocBase bloc, Change change) {
    super.onChange(bloc, change);
    logger.i("${bloc.runtimeType} $change");
  }
}
