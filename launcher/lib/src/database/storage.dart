import 'dart:io';

import 'package:path/path.dart';
import 'package:path_provider/path_provider.dart';

class DirectoryGetter {
  Future<String> getMediaDirectory() async {
    Directory applicationDirectory =
        Directory((await getApplicationDocumentsDirectory()).path);

    final mediaDirectoryPath = join(applicationDirectory.path, "media");

    final mediaDir = Directory(mediaDirectoryPath);

    if (!(await mediaDir.exists())) {
      await mediaDir.create();
    }

    return mediaDirectoryPath;
  }

  Future<String> getLogDirectory() async {
    Directory applicationDirectory =
        Directory((await getApplicationSupportDirectory()).path);
    final logDirectoryPath = join(applicationDirectory.path, "logs");
    final logDir = Directory(logDirectoryPath);

    if (!(await logDir.exists())) {
      await logDir.create();
    }
    return logDirectoryPath;
  }
}
