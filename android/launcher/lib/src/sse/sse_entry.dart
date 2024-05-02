import 'dart:isolate';
import 'package:eventflux/client.dart';
import 'package:eventflux/enum.dart';
import 'package:eventflux/models/response.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/sse/processing/processor.dart';

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

void sseEntryPoint(SendPort port) async {
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
    response?.stream?.listen((event) {
      final sseData =
          event.data.trim().replaceAll("'", "").replaceAll("\"", "");
      logger.i("SSE received: '$sseData'.");
      Isolate.spawn(processSSE, sseData.toSSE());
    });
  }, onError: (error) {
    logger.e("${error.message}");
  }, autoReconnect: true);
}

void processSSE(SSE message) async {
  final processorFactory = ProcessorFactory();
  Processor? processor = processorFactory.create(message);
  if (processor != null) {
    var success = await processor.process();
    if (!success) {
      logger.e("Unsuccessful processing for ${message.toString()}");
    }
  } else {
    logger.w("No processor for ${message.toString()}");
  }
}
