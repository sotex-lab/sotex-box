import 'dart:async';
import 'package:eventflux/client.dart';
import 'package:eventflux/enum.dart';
import 'package:eventflux/models/response.dart';
import 'package:launcher/src/common/notification.dart';
import 'package:launcher/src/sse/processing/processor.dart';
import 'package:launcher/src/sse/processing/processor_factory.dart';

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

Future<void> startListeningForSSE() async {
  const deviceId = String.fromEnvironment("device_id");
  if (deviceId == "") {
    throw Exception("Device id is not defined.");
  }
  const backendHost = String.fromEnvironment("backend_host");
  if (backendHost == "") {
    throw Exception("Backend host is not defined.");
  }
  const url = "$backendHost/event/connect?id=$deviceId";
  EventFlux.instance.connect(EventFluxConnectionType.get, url,
      onSuccessCallback: (EventFluxResponse? response) {
    response?.stream?.listen((event) async {
      final sseData =
          event.data.trim().replaceAll("'", "").replaceAll("\"", "");
      Notification().i("SSE received: '$sseData'.");
      processSSE(sseData.toSSE());
    });
  }, onError: (error) async {
    Notification().i("SSE received: '${error.message}'.");
  }, autoReconnect: true);
}

void processSSE(SSE message) async {
  final processorFactory = ProcessorFactory();
  Processor? processor = await processorFactory.create(message);
  if (processor != null) {
    var success = await processor.process();
    if (!success) {
      Notification().e("Unsuccessful processing for ${message.toString()}");
    }
  } else {
    Notification().w("No processor for ${message.toString()}");
  }
}
