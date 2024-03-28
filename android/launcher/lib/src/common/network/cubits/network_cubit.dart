import 'dart:async';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:connectivity_plus/connectivity_plus.dart';

enum NetworkState { online, offline }

class NetworkCubit extends Cubit<NetworkState> {
  final Connectivity _connectivity = Connectivity();
  late StreamSubscription<List<ConnectivityResult>> _connectivitySubscription;

  NetworkCubit() : super(NetworkState.offline) {
    _connectivitySubscription =
        _connectivity.onConnectivityChanged.listen(_updateState);
    _checkCurrentConnectivity();
  }

  void _updateState(List<ConnectivityResult> result) {
    if (result.contains(ConnectivityResult.ethernet) ||
        result.contains(ConnectivityResult.wifi) ||
        result.contains(ConnectivityResult.mobile)) {
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
