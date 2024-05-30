import 'dart:io';

import 'package:launcher/src/common/debug_singleton.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/database/storage.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:path/path.dart';

Future<String> getMediaPathForItem(ScheduleItem item) async {
  final mediaDirPath = await DirectoryGetter().getMediaDirectory();
  final mediaDir = Directory(mediaDirPath);

  if (!(await mediaDir.exists())) {
    await mediaDir.create();
  }

  final itemDir = join(mediaDirPath, "${item.ad.id}.mp4");
  return itemDir;
}

Future<String?> getPathIfExistsForItem(ScheduleItem item) async {
  final itemPath = await getMediaPathForItem(item);
  var file = File(itemPath);
  DebugSingleton().getDebugBloc.add(DebugPushEvent("File: $itemPath"));

  if (!await file.exists()) {
    return null;
  }

  return itemPath;
}
