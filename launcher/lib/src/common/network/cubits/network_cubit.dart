import 'dart:async';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:connectivity_plus/connectivity_plus.dart';

enum NetworkState { online, offline }

class NetworkCubit extends Cubit<NetworkState> {
  late Connectivity _connectivity;

  late StreamSubscription<List<ConnectivityResult>> _connectivitySubscription;

  NetworkCubit() : super(NetworkState.offline) {
    _connectivity = Connectivity();
    _connectivity.onConnectivityChanged.listen(updateState);
  }

  NetworkCubit.params(Stream<List<ConnectivityResult>> connectivityStream,
      Connectivity connectivity)
      : super(NetworkState.offline) {
    _connectivitySubscription = connectivityStream.listen(updateState);
    _connectivity = connectivity;
  }

  void updateState(List<ConnectivityResult> result) {
    if (result.contains(ConnectivityResult.ethernet) ||
        result.contains(ConnectivityResult.wifi) ||
        result.contains(ConnectivityResult.mobile)) {
      emit(NetworkState.online);
    } else {
      emit(NetworkState.offline);
    }
  }

  Future<List<ConnectivityResult>> checkConnectivity() async {
    return await _connectivity.checkConnectivity();
  }

  @override
  Future<void> close() {
    _connectivitySubscription.cancel();
    return super.close();
  }
}
