import 'dart:async';
import 'dart:isolate';
import 'package:eventflux/client.dart';
import 'package:eventflux/enum.dart';
import 'package:eventflux/models/response.dart';
import 'package:flutter/services.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/sse/processing/processor.dart';
import 'package:launcher/src/sse/processing/processor_factory.dart';
import 'package:tuple/tuple.dart';

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

Future<RootIsolateToken> waitForRootToken(ReceivePort receivePort) {
  Completer<RootIsolateToken> completer = Completer<RootIsolateToken>();

  receivePort.listen((message) {
    if (message is RootIsolateToken) {
      completer.complete(message);
    }
  });

  return completer.future;
}

Future<SSE> waitForSSE(ReceivePort receivePort) {
  Completer<SSE> completer = Completer<SSE>();

  receivePort.listen((message) {
    if (message is SSE) {
      completer.complete(message);
    }
  });

  return completer.future;
}

Future<Tuple2<RootIsolateToken, SSE>> waitForTokenAndSSE(
    ReceivePort receivePort) async {
  var tokenCompleter = Completer<RootIsolateToken>();
  var sseCompleter = Completer<SSE>();

  receivePort.listen((message) {
    if (message is RootIsolateToken) {
      tokenCompleter.complete(message);
    } else if (message is SSE) {
      sseCompleter.complete(message);
    }
  });

  return Tuple2(await tokenCompleter.future, await sseCompleter.future);
}

Future<SendPort> waitForSendPort(ReceivePort receivePort) {
  Completer<SendPort> completer = Completer<SendPort>();

  receivePort.listen((message) {
    if (message is SendPort) {
      completer.complete(message);
    }
  });

  return completer.future;
}

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
      logger.i("SSE received: '$sseData'.");
      processSSE(sseData.toSSE());
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
