import 'dart:convert';
import 'dart:io';
import 'dart:isolate';
import 'package:dio/dio.dart';
import 'package:launcher/src/common/dio_access.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/database/media.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:launcher/src/sse/processing/processor.dart';
import 'package:launcher/src/sse/providers/schedule_item_provider.dart';
import 'package:launcher/src/sse/sse_entry.dart';
import 'package:path/path.dart';

class ScheduleProcessor extends Processor {
  ScheduleProcessor(SSE message) : super(message);

  @override
  Future<bool> process() async {
    try {
      final scheduleDownloadUrl = await getScheduleDownloadUrl(dio);
      final DeviceSchedule deviceSchedule =
          await downloadSchedule(dio, scheduleDownloadUrl);
      final insertCount = await saveScheduleToDatabase(deviceSchedule);
      isolateDownloadMedia(deviceSchedule);
      logger.d("Inserted $insertCount schedule items in the database.");
    } catch (e) {
      logger.e(e);
      return false;
    }

    logger.i("Schedule message processed.");
    return true;
  }
}

Future<String> getScheduleDownloadUrl(Dio dio) async {
  const deviceId = String.fromEnvironment("device_id");
  if (deviceId == "") {
    throw Exception("Device id is not defined.");
  }
  const backendHost = String.fromEnvironment("backend_host");
  if (backendHost == "") {
    throw Exception("Backend host is not defined.");
  }

  var url = "$backendHost/schedule/$deviceId";
  final res = await dio.get(url);
  final scheduleDownloadUrl = res.data.toString();

  return scheduleDownloadUrl;
}

Future<DeviceSchedule> downloadSchedule(
    Dio dio, String scheduleDownloadUrl) async {
  final res2 = await dio.get(scheduleDownloadUrl);
  final schedule = res2.data.toString();
  final scheduleJson = jsonDecode(schedule) as Map<String, dynamic>;
  final deviceSchedule = DeviceSchedule.fromJson(scheduleJson);
  return deviceSchedule;
}

Future<int> saveScheduleToDatabase(DeviceSchedule deviceSchedule) async {
  final provider = ScheduleItemProvider();
  final count = await provider.insertBatch(deviceSchedule.schedule);
  return count;
}

Future<void> downloadScheduleMedia(DeviceSchedule deviceSchedule) async {
  Isolate.spawn(isolateDownloadMedia, deviceSchedule);
}

isolateDownloadMedia(DeviceSchedule deviceSchedule) async {
  final Dio dio = Dio();
  for (final item in deviceSchedule.schedule) {
    final itemPath = await getMediaPathForItem(item);
    var file = File(itemPath);
    if (!await file.exists()) {
      logger.d("File directory: '$itemPath'.");
      logger.i("Downloading: '$item'.");
      await dio.download(
          item.downloadLink, join(itemPath, "${item.ad.id}.mp4"));
    } else {
      logger.i("Already downloaded: '$item'.");
    }
  }
}
