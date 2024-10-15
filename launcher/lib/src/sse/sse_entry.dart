import 'dart:async';
import 'package:eventflux/client.dart';
import 'package:eventflux/enum.dart';
import 'package:eventflux/models/reconnect.dart';
import 'package:eventflux/models/response.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/common/notification.dart' as launcher_notification;
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:launcher/src/sse/processing/processor.dart';
import 'package:launcher/src/sse/processing/processor_factory.dart';
import 'package:shared_preferences/shared_preferences.dart';
extension SSEConverting on String {
  SSE toSSE() {
    switch (this) {
      case "0":
        return SSENoopMessage();
      case "1":
        return SSEScheduleMessage();
      default:
        return SSEUndefinedMessage();
    }
  }
}

abstract class SSE {}

class SSEUndefinedMessage implements SSE {}

class SSENoopMessage implements SSE {}

class SSEScheduleMessage implements SSE {}

Future<void> startListeningForSSE(BuildContext context, {String? deviceId}) async {
  final prefs = await SharedPreferences.getInstance();
  deviceId ??= prefs.getString('deviceId') ?? '';
  if (deviceId.isEmpty && context.mounted) {
    BlocProvider.of<NavigationCubit>(context).goToDeviceRegistration();
    return;
  }
  const backendHost = String.fromEnvironment("backend_host");
  if (backendHost == "") {
    throw Exception("Backend host is not defined.");
  }
  String url = "$backendHost/event/connect?id=$deviceId";
  EventFlux.instance.connect(EventFluxConnectionType.get, url,
        onSuccessCallback: (EventFluxResponse? response) {
      response?.stream?.listen((event) async {
        final sseData =
            event.data.trim().replaceAll("'", "").replaceAll("\"", "");
        launcher_notification.Notification().i("SSE received: '$sseData'.");
        processSSE(sseData.toSSE());
      });
    }, onError: (error) async {
      launcher_notification.Notification().i("SSE received: '${error.message}'.");
    },
    autoReconnect: true,
    reconnectConfig: ReconnectConfig(
      mode: ReconnectMode.linear,
      interval: const Duration(seconds: 2),
      maxAttempts: 5,
    ),
  );
}

void processSSE(SSE message) async {
  final processorFactory = ProcessorFactory();
  Processor? processor = await processorFactory.create(message);
  if (processor != null) {
    var success = await processor.process();
    if (!success) {
      launcher_notification.Notification().e("Unsuccessful processing for ${message.toString()}");
    }
  } else {
    launcher_notification.Notification().w("No processor for ${message.toString()}");
  }
}
