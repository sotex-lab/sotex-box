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
    MaterialPage<void> wifiPickerPage = const MaterialPage(
        child: WifiPickerPage(), key: ValueKey('WifiPickerPage'));

    MaterialPage<void> channelPage = const MaterialPage(
        child: ChannelPage(), key: ValueKey('ChannelPickerPage'));

    List<MaterialPage<void>> pages = [wifiPickerPage, channelPage];

    return BlocBuilder<NavigationCubit, NavigationState>(
        builder: (context, navigationState) =>
            BlocBuilder<NetworkCubit, NetworkState>(
              builder: (context, networkState) {
                return Navigator(
                  key: navigatorKey,
                  pages: [
                    if (networkState == NetworkState.online) channelPage,
                    if (networkState == NetworkState.offline) wifiPickerPage
                  ],
                  onDidRemovePage: (Page<Object?> page) {
                    pages.remove(page);
                    pages = pages.toList();
                  },
                );
              },
            ));
  }

  @override
  Future<void> setNewRoutePath(NavigationState configuration) async {}
}
