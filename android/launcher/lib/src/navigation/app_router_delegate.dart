import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';

class AppRouterDelegate extends RouterDelegate<NavigationState>
    with ChangeNotifier, PopNavigatorRouterDelegateMixin<NavigationState> {
  @override
  final GlobalKey<NavigatorState> navigatorKey;

  AppRouterDelegate() : navigatorKey = GlobalKey<NavigatorState>();

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<NavigationCubit, NavigationState>(
        builder: (context, navigationState) =>
            BlocBuilder<NetworkCubit, NetworkState>(
              builder: (context, networkState) {
                return Navigator(
                  key: navigatorKey,
                  pages: [
                    if (networkState == NetworkState.online)
                      const MaterialPage(
                          child: ChannelPickerPage(),
                          key: ValueKey('ChannelPickerPage')),
                    if (networkState == NetworkState.offline)
                      const MaterialPage(
                          child: WifiPickerPage(),
                          key: ValueKey('WifiPickerPage')),
                  ],
                  onPopPage: (route, result) => route.didPop(result),
                );
              },
            ));
  }

  @override
  Future<void> setNewRoutePath(NavigationState configuration) async {}
}
