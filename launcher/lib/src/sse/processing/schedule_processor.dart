import 'dart:convert';
import 'dart:io';
import 'dart:isolate';
import 'package:dio/dio.dart';
import 'package:launcher/src/database/media.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:launcher/src/sse/processing/processor.dart';
import 'package:launcher/src/sse/providers/schedule_item_provider.dart';
import 'package:launcher/src/sse/sse_entry.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../common/notification.dart';

class ScheduleProcessor extends Processor {
  ScheduleProcessor(SSE message) : super(message);

  @override
  Future<bool> process() async {
    try {
      final scheduleDownloadUrl = await getScheduleDownloadUrl();
      final DeviceSchedule deviceSchedule = await downloadSchedule(scheduleDownloadUrl);
      final insertCount = await saveScheduleToDatabase(deviceSchedule);
      // isolateDownloadMedia(deviceSchedule);
      Notification().i("Inserted $insertCount schedule items in the database.");
    } catch (e) {
      Notification().e(e.toString());
      Notification().e("FAILED TO DOWNLOAD SCHEDULE");
      return false;
    }

    Notification().i("Schedule message processed.");
    return true;
  }
}

Future<String> getScheduleDownloadUrl() async {
  final prefs = await SharedPreferences.getInstance();
  String deviceId = prefs.getString('deviceId') ?? '';
  if (deviceId == "") {
    throw Exception("Device id is not defined.");
  }
  const backendHost = String.fromEnvironment("backend_host");
  if (backendHost == "") {
    throw Exception("Backend host is not defined.");
  }

  var url = "$backendHost/api/schedule/$deviceId";
  final res = await Dio().get(url);
  final scheduleDownloadUrl = res.data.toString();

  return scheduleDownloadUrl;
}

Future<DeviceSchedule> downloadSchedule(String scheduleDownloadUrl) async {
  final res2 = await Dio().get(scheduleDownloadUrl);
  final schedule = res2.data.toString();
  final scheduleJson = jsonDecode(schedule) as Map<String, dynamic>;
  final deviceSchedule = DeviceSchedule.fromJson(scheduleJson);
  // await isolateDownloadMedia(deviceSchedule);
  downloadScheduleMedia(deviceSchedule);
  return deviceSchedule;
}

Future<int> saveScheduleToDatabase(DeviceSchedule deviceSchedule) async {
  final provider = ScheduleItemProvider();
  final count = await provider.insertBatch(deviceSchedule.schedule);
  return count;
}

Future<void> downloadScheduleMedia(DeviceSchedule deviceSchedule) async {
  for (final item in deviceSchedule.schedule) {
    final itemPath = await getMediaPathForItem(item);
    var file = File(itemPath);
    if (!await file.exists()) {
      Notification().i("Started download '$item'.");
      Isolate.spawn(isolateDownloadMedia,[item, itemPath]);
    }
    else{
      Notification().i("Already downloaded: '$item'.");
    }
  }
}

void isolateDownloadMedia(List<dynamic> args) async {
  final ScheduleItem item = args[0] as ScheduleItem;
  final String itemPath = args[1] as String;

  try {
    await Dio().download(item.downloadLink, itemPath);
  } catch (e) {
    print("Faild to download video");
  }
}
