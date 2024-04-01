import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';

class SotexBoxRouterDelegate extends RouterDelegate<NavigationState>
    with ChangeNotifier, PopNavigatorRouterDelegateMixin<NavigationState> {
  final GlobalKey<NavigatorState> navigatorKey;
  final NavigationCubit navigationCubit;

  SotexBoxRouterDelegate(this.navigationCubit)
      : navigatorKey = GlobalKey<NavigatorState>() {
    navigationCubit.stream.listen((event) {
      notifyListeners();
    });
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<NavigationCubit, NavigationState>(
      bloc: navigationCubit,
      builder: (context, state) {
        return Navigator(
          key: navigatorKey,
          pages: [
            if (state is WiFiPicker)
              const MaterialPage(child: WifiPickerPage()),
            if (state is ChannelPicker)
              const MaterialPage(child: ChannelPickerPage())
          ],
          onPopPage: (route, result) => route.didPop(result),
        );
      },
    );
  }

  @override
  Future<void> setNewRoutePath(NavigationState configuration) async {}
}
