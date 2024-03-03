import 'package:bloc/bloc.dart';
import 'package:launcher/counter_bloc.dart';
import 'package:launcher/logger.dart';
import 'package:launcher/simple_bloc_observer.dart';

Stream<int> integerStream() async* {
  for (int i = 0; i < 3; i++) {
    await Future.delayed(const Duration(seconds: 1));
    yield i;
  }
}

void consumeStream(Stream<int> stream) async {
  Stream<int> stream = integerStream();

  await for (int i in stream) {
    logger.i(i);
  }
}

class CounterCubit extends Cubit<int> {
  CounterCubit() : super(0);
  CounterCubit.parameters(int initialState) : super(initialState);

  void increment() {
    // addError(Exception("increment error!"), StackTrace.current);
    emit(state + 1);
  }

  @override
  void onChange(Change<int> change) {
    super.onChange(change);
    logger.i("$change");
  }

  @override
  void onError(Object error, StackTrace stackTrace) {
    logger.e("$error, $stackTrace");
    super.onError(error, stackTrace);
  }
}

void testCubits() {
  final cubit = CounterCubit();
  final cubitParam = CounterCubit.parameters(5);

  cubit.increment();
  cubitParam.increment();
  cubit.close();
}

void testBloc() async {
  final bloc = CounterBloc();
  logger.i("${bloc.state}");
  bloc.add(CounterIncrementPressed());
  await Future.delayed(Duration.zero);
  logger.i("${bloc.state}");
  bloc.close();
}

void main() async {
  // consumeStream(integerStream());
  Bloc.observer = SimpleBlockObserver();
  // testCubits();
  testBloc();
}
