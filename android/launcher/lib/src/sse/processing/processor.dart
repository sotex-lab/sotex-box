import 'dart:convert';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:launcher/src/sse/sse_entry.dart';
import 'package:dio/dio.dart';

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
      final res =
          await dio.get(url); // TODO: Think about top level client pool.
      final scheduleDownloadUrl = res.data.toString();
      logger.d("Schedule download url: $scheduleDownloadUrl");
      final res2 = await dio.get(scheduleDownloadUrl);
      final schedule = res2.data.toString();
      final scheduleJson = jsonDecode(schedule) as Map<String, dynamic>;
      final deviceSchedule = DeviceSchedule.fromJson(scheduleJson);
      logger.d("Schedule response: ${deviceSchedule.toString()}");
    } catch (e) {
      logger.e(e);
      return false;
    }

    logger.i("Schedule message processed");
    return true;
  }
}

class ProcessorFactory {
  Processor? create(SSE message) {
    logger.d("Creating processor for: ${message.toString()}");
    if (message is SSEScheduleMessage) {
      return ScheduleProcessor(message);
    }

    return null;
  }
}
