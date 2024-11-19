import 'package:launcher/src/sse/models/schedule.dart';
import 'package:sqflite/sqflite.dart';

import '../../common/notification.dart';
import '../../database/database.dart';

class ScheduleItemProvider {
  final BoxDatabase boxDatabase = BoxDatabase();

  ScheduleItemProvider();

  Future<void> insert(ScheduleItem item) async {
    Database database = await boxDatabase.get();
    await database.insert(tableScheduleItems, item.toMap());
  }

  Future<int> insertBatch(List<ScheduleItem> items) async {
    try {
      Database database = await boxDatabase.get();
      await database.transaction((txn) async {
        var batch = txn.batch();

        for (var it in items) {
          batch.insert(tableScheduleItems, it.toMap());
        }

        // await txn.delete(tableScheduleItems, where: "_createdAt < datetime('now', '-1 day')");
        
        await batch.commit();
      });

      return items.length;
    } catch (e) {
      Notification().e(e.toString());
      return 0;
    }
  }

  // Future<void> removeOldEntries() async {
  //   Database database = await boxDatabase.get();
  //   await database.transaction((txn) async {
  //     await txn.delete(tableScheduleItems,
  //         where: "_createdAt < datetime('now', '-1 day')");
  //   });
  // }

  Future<List<ScheduleItem>> getScheduleItems() async {
    Database database = await boxDatabase.get();
    final List<Map<String, dynamic>> maps =
        await database.query(tableScheduleItems);
    var result = maps
        .map((map) => ScheduleItem(
            downloadLink: map[columnUrl],
            ad: Ad(
                scope: map[columnScope],
                tags: map[columnTags].split(','),
                id: map[columnAdId])))
        .toList();

    return result;
  }
}
