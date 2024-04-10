import 'dart:isolate';
import 'package:eventflux/client.dart';
import 'package:eventflux/enum.dart';
import 'package:eventflux/models/exception.dart';
import 'package:eventflux/models/response.dart';
import 'package:launcher/src/common/logger.dart';

extension SSEConverting on String {
  SSE toSSE() {
    switch (this) {
      case "noop":
        return SSENoopMessage();
      default:
        return SSEUndefinedMessage();
    }
  }
}

abstract class SSE {}

class SSEUndefinedMessage implements SSE {}

class SSENoopMessage implements SSE {}

void sseEntryPoint(SendPort port) async {
  const url = String.fromEnvironment("sse_url",
      defaultValue:
          "http://10.0.2.2:8000/event/connect?id=1218cbbc-f2d7-4a06-b8d9-d8d6d3732663");
  if (url == "") {
    throw Exception("SSE URL is not defined.");
  }
  EventFlux.instance.connect(EventFluxConnectionType.get, url,
      onSuccessCallback: (EventFluxResponse? response) {
    response?.stream?.listen((event) {
      final sseData = event.data.trim().replaceAll("\"", "");
      logger.i("SSE received: '$sseData'.");
      Isolate.spawn(processSSE, sseData.toSSE());
    });
  }, onError: (error) {
    logger.e("${error.message}");
  }, autoReconnect: true);
}

void processSSE(SSE message) {
  if (message is SSENoopMessage) {
    logger.i("Noop message processed.");
  } else {
    logger.w("Undefined message processed.");
  }
}
