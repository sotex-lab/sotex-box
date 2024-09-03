import 'package:launcher/src/sse/processing/processor.dart';
import 'package:launcher/src/sse/processing/schedule_processor.dart';
import 'package:launcher/src/sse/sse_entry.dart';

import '../../common/notification.dart';

class ProcessorFactory {
  Future<Processor?> create(SSE message) async {
    Notification().i("Creating processor for: ${message.toString()}.");
    if (message is SSEScheduleMessage) {
      return ScheduleProcessor(message);
    }

    return null;
  }
}
