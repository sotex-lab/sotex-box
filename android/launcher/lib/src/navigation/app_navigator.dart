import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';

class AppNavigator extends StatelessWidget {
  const AppNavigator({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<NavigationCubit, NavigationState>(
        builder: (context, navigationState) =>
            BlocBuilder<NetworkCubit, NetworkState>(
                builder: (context, networkState) => Navigator(pages: [
                      if (navigationState is WiFiPicker ||
                          networkState == NetworkState.offline)
                        const MaterialPage(child: WifiPickerPage()),
                      if (navigationState is ChannelPicker ||
                          networkState == NetworkState.online)
                        const MaterialPage(child: ChannelPickerPage())
                    ], onPopPage: (route, result) => route.didPop(result))));
  }
}
