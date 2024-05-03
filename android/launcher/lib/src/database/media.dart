import 'dart:io';

import 'package:launcher/src/sse/models/schedule.dart';
import 'package:path/path.dart';
import 'package:path_provider/path_provider.dart';

Future<String> getMediaDirectory() async {
  Directory applicationDirectory = await getApplicationDocumentsDirectory();
  final mediaDirectory = join(applicationDirectory.path, "media");
  return mediaDirectory;
}

Future<String> getMediaPathForItem(ScheduleItem item) async {
  final mediaDir = await getMediaDirectory();
  final itemDir = join(mediaDir, "${item.ad.id}.mp4");
  return itemDir;
}

Future<String?> getPathIfExistsForItem(ScheduleItem item) async {
  final itemPath = await getMediaPathForItem(item);
  var file = File(itemPath);

  if (!await file.exists()) {
    return null;
  }

  return itemPath;
}
