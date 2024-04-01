import 'package:flutter/material.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';

class SotexBoxInformationParser
    extends RouteInformationParser<NavigationState> {
  @override
  Future<NavigationState> parseRouteInformation(
      RouteInformation routeInformation) async {
    final uri = routeInformation.uri;

    if (uri.pathSegments.isEmpty) {
      return WiFiPicker();
    }

    switch (uri.pathSegments[0]) {
      case 'WiFiPicker':
        return WiFiPicker();
      case 'ChannelPicker':
        return ChannelPicker();
      default:
        return WiFiPicker();
    }
  }

  @override
  RouteInformation? restoreRouteInformation(NavigationState configuration) {
    if (configuration is WiFiPicker) {
      return RouteInformation(uri: Uri.parse('/WiFiPicker'));
    } else if (configuration is ChannelPicker) {
      return RouteInformation(uri: Uri.parse('/ChannelPicker'));
    }

    return null;
  }
}
