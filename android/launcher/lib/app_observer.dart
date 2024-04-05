import 'package:bloc/bloc.dart';
import 'src/common/logger.dart';

/// [BlocObserver] for the application which
/// observes all state changes
class AppObserver extends BlocObserver {
  const AppObserver() : super();

  @override
  void onChange(BlocBase bloc, Change change) {
    super.onChange(bloc, change);
    logger.i("${bloc.runtimeType} $change");
  }
}
