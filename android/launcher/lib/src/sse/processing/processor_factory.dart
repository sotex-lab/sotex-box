import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/sse/processing/processor.dart';
import 'package:launcher/src/sse/processing/schedule_processor.dart';
import 'package:launcher/src/sse/sse_entry.dart';

class ProcessorFactory {
  Processor? create(SSE message) {
    logger.d("Creating processor for: ${message.toString()}.");
    if (message is SSEScheduleMessage) {
      return ScheduleProcessor(message);
    }

    return null;
  }
}
