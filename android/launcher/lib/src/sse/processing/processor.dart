import 'dart:convert';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:launcher/src/sse/providers/schedule_item_provider.dart';
import 'package:launcher/src/sse/sse_entry.dart';
import 'package:dio/dio.dart';
import 'package:path/path.dart';
import 'package:path_provider/path_provider.dart';

final dio = Dio();

abstract class Processor {
  final SSE message;

  Processor(this.message);

  Future<bool> process() async {
    return true;
  }
}

class ScheduleProcessor extends Processor {
  ScheduleProcessor(SSE message) : super(message);

  @override
  Future<bool> process() async {
    // It should fail if these are not defined, hence not in try catch block.
    const deviceId = String.fromEnvironment("device_id");
    if (deviceId == "") {
      throw Exception("Device id is not defined.");
    }
    const backendHost = String.fromEnvironment("backend_host");
    if (backendHost == "") {
      throw Exception("Backend host is not defined.");
    }

    var url = "$backendHost/schedule/$deviceId";

    try {
      final res = await dio.get(url);
      final scheduleDownloadUrl = res.data.toString();
      final res2 = await dio.get(scheduleDownloadUrl);
      final schedule = res2.data.toString();
      final scheduleJson = jsonDecode(schedule) as Map<String, dynamic>;
      final deviceSchedule = DeviceSchedule.fromJson(scheduleJson);
      final provider = ScheduleItemProvider();
      logger.d("Hello 2");
      final appDirectory = await getApplicationDocumentsDirectory();
      await provider.open(join(appDirectory.path, 'sotex_box.db'));
      logger.d("Hello 3");
      final count = provider.insertBatch(deviceSchedule.schedule);
      logger.d("Hello 4");
      logger.d("Inserted $count schedule items in the database.");
    } catch (e) {
      logger.e(e);
      return false;
    }

    logger.i("Schedule message processed.");
    return true;
  }
}

class ProcessorFactory {
  Processor? create(SSE message) {
    logger.d("Creating processor for: ${message.toString()}.");
    if (message is SSEScheduleMessage) {
      return ScheduleProcessor(message);
    }

    return null;
  }
}
