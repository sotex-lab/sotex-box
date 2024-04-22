import 'dart:async';

import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';
import 'package:launcher/src/common/common.dart';
import 'package:launcher/src/common/network/cubits/network_cubit.dart';
import 'package:mocktail/mocktail.dart';
import 'package:test/test.dart';
import 'package:bloc_test/bloc_test.dart';

class MockConnectivity extends Mock implements Connectivity {}

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  group(NetworkCubit, () {
    late Connectivity connectivity;
    late Stream<List<ConnectivityResult>> connectivityStream;
    late NetworkCubit networkCubit;

    setUp(() {
      connectivity = MockConnectivity();
      connectivityStream =
          StreamController<List<ConnectivityResult>>.broadcast().stream;
      networkCubit = NetworkCubit.params(connectivityStream, connectivity);
    });

    tearDown(() => networkCubit.close());

    test("initial state is offline", () {
      expect(networkCubit.state, equals(NetworkState.offline));
    });

    blocTest(
        'emits online network state when the device has ethernet connectivity',
        build: () {
          when(() => connectivity.onConnectivityChanged)
              .thenAnswer((_) => connectivityStream);
          when(() => connectivity.checkConnectivity())
              .thenAnswer((_) => Future.value([ConnectivityResult.ethernet]));
          networkCubit = NetworkCubit.params(connectivityStream, connectivity);

          return networkCubit;
        },
        act: (networkCubit) async {
          var state = await networkCubit.checkConnectivity();
          networkCubit.updateState(state);
        },
        expect: () => [NetworkState.online]);

    blocTest('emits online network state when the device has wifi connectivity',
        build: () {
          when(() => connectivity.onConnectivityChanged)
              .thenAnswer((_) => connectivityStream);
          when(() => connectivity.checkConnectivity())
              .thenAnswer((_) => Future.value([ConnectivityResult.wifi]));
          networkCubit = NetworkCubit.params(connectivityStream, connectivity);

          return networkCubit;
        },
        act: (networkCubit) async {
          var state = await networkCubit.checkConnectivity();
          networkCubit.updateState(state);
        },
        expect: () => [NetworkState.online]);

    blocTest(
        'emits online network state when the device has mobile connectivity',
        build: () {
          when(() => connectivity.onConnectivityChanged)
              .thenAnswer((_) => connectivityStream);
          when(() => connectivity.checkConnectivity())
              .thenAnswer((_) => Future.value([ConnectivityResult.mobile]));
          networkCubit = NetworkCubit.params(connectivityStream, connectivity);

          return networkCubit;
        },
        act: (networkCubit) async {
          var state = await networkCubit.checkConnectivity();
          networkCubit.updateState(state);
        },
        expect: () => [NetworkState.online]);

    blocTest('emits offline network state when the device has no connectivity',
        build: () {
          when(() => connectivity.onConnectivityChanged)
              .thenAnswer((_) => connectivityStream);
          when(() => connectivity.checkConnectivity())
              .thenAnswer((_) => Future.value([]));
          networkCubit = NetworkCubit.params(connectivityStream, connectivity);

          return networkCubit;
        },
        act: (networkCubit) async {
          var state = await networkCubit.checkConnectivity();
          networkCubit.updateState(state);
        },
        expect: () => [NetworkState.offline]);
  });
}
