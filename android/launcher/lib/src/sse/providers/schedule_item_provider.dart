import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:sqflite/sqflite.dart';

const tableScheduleItems = "schedule_items";
const columnId = "_id";
const columnUrl = "url";
const columnScope = "scope";
const columnTags = "tags";
const columnAdId = "adId";
const createdAt = "_createdAt";

class ScheduleItemProvider {
  late Database db;

  Future open(String path) async {
    logger.d("Database path: $path");
    db = await openDatabase(path, version: 1,
        onCreate: (Database db, int version) async {
      await db.execute('''
          create table if not exists $tableScheduleItems (
          $columnId INTEGER PRIMARY KEY AUTOINCREMENT,
          $createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,
          $columnUrl TEXT NOT NULL,
          $columnScope INTEGER NOT NULL,
          $columnTags TEXT NOT NULL,
          $columnAdId TEXT NOT NULL
          );
          ''');
    });
  }

  Future<void> insert(ScheduleItem item) async {
    await db.insert(tableScheduleItems, item.toMap());
  }

  Future<int> insertBatch(List<ScheduleItem> items) async {
    try {
      await db.transaction((txn) async {
        var batch = txn.batch();

        for (var it in items) {
          batch.insert(tableScheduleItems, it.toMap());
        }

        await batch.commit();
      });

      return items.length;
    } catch (e) {
      logger.e(e);
      return 0;
    }
  }

  Future<void> removeOldEntries() async {
    await db.transaction((txn) async {
      await txn.delete(tableScheduleItems,
          where: '_createdAt < DATE_SUB(NOW(), INTERVAL 1 DAY)');
    });
  }

  Future<List<ScheduleItem>> getScheduleItems() async {
    final List<Map<String, dynamic>> maps = await db.query(tableScheduleItems);
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
