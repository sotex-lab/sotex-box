import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/channels/views/diagnostic_view_page.dart';
import 'package:launcher/src/common/device_registration.dart';
import 'package:launcher/src/common/network/network.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';

class AppNavigator extends StatelessWidget {
  const AppNavigator({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<NavigationCubit, NavigationState>(
        builder: (context, navigationState) =>
            BlocBuilder<NetworkCubit, NetworkState>(
                builder: (context, networkState) {
              var pages = [
                if (navigationState is WiFiPicker ||
                    networkState == NetworkState.offline)
                  const MaterialPage(child: WifiPickerPage()),
                if (navigationState is ChannelPicker ||
                    networkState == NetworkState.online)
                  const MaterialPage(child: ChannelPage()),
                if(navigationState is Diagnostics)
                  MaterialPage(child: DiagnosticsViewer()),
                if(navigationState is DeviceRegistration)
                  MaterialPage(child: DeviceRegistrationPage())
              ];
              return Navigator(
                  pages: pages,
                  onDidRemovePage: (Page<Object?> page) {
                    pages.remove(page);
                  });
            }));
  }
}

// final pages = [
//   if (navigationState is WiFiPicker || networkState == NetworkState.offline)
//     const MaterialPage(child: WifiPickerPage()),
//   if (navigationState is ChannelPicker ||
//       networkState == NetworkState.online)
//     const MaterialPage(child: ChannelPage())
// ];
