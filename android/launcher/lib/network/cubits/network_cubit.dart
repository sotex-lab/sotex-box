import 'dart:async';

import 'package:bloc/bloc.dart';
import 'package:connectivity_plus/connectivity_plus.dart';

enum NetworkState { online, offline }

class NetworkCubit extends Cubit<NetworkState> {
  final Connectivity _connectivity = Connectivity();
  late StreamSubscription<ConnectivityResult> _connectivitySubscription;

  NetworkCubit() : super(NetworkState.offline) {
    _connectivity.onConnectivityChanged.listen(_updateState);
  }

  void _updateState(ConnectivityResult result) {
    if (result == ConnectivityResult.ethernet ||
        result == ConnectivityResult.wifi ||
        result == ConnectivityResult.mobile) {
      emit(NetworkState.online);
    } else {
      emit(NetworkState.offline);
    }
  }

  Future<void> _checkCurrentConnectivity() async {
    var result = await _connectivity.checkConnectivity();
    _updateState(result);
  }

  @override
  Future<void> close() {
    _connectivitySubscription.cancel();
    return super.close();
  }
}
