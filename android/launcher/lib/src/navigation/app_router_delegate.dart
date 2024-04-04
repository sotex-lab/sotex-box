import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/common/network/cubits/network_cubit.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';

class AppRouterDelegate extends RouterDelegate<NavigationState>
    with ChangeNotifier, PopNavigatorRouterDelegateMixin<NavigationState> {
  @override
  final GlobalKey<NavigatorState> navigatorKey;
  // final NavigationCubit navigationCubit;
  // final NetworkCubit networkCubit;

  AppRouterDelegate() : navigatorKey = GlobalKey<NavigatorState>() {
    // navigationCubit.stream.listen((event) {
    //   notifyListeners();
    // });

    // networkCubit.stream.listen((event) {
    //   notifyListeners();
    // });
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<NavigationCubit, NavigationState>(
        builder: (context, navigationState) =>
            BlocBuilder<NetworkCubit, NetworkState>(
              builder: (context, networkState) {
                return Navigator(
                  key: navigatorKey,
                  pages: [
                    if (navigationState is WiFiPicker ||
                        networkState == NetworkState.online)
                      const MaterialPage(child: WifiPickerPage()),
                    if (navigationState is ChannelPicker)
                      const MaterialPage(child: ChannelPickerPage())
                  ],
                  onPopPage: (route, result) => route.didPop(result),
                );
              },
            ));
  }

  @override
  Future<void> setNewRoutePath(NavigationState configuration) async {}
}
