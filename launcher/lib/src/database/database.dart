import 'package:path/path.dart';
import 'package:path_provider/path_provider.dart';
import 'package:sqflite/sqflite.dart';

const tableScheduleItems = "schedule_items";
const columnId = "_id";
const columnUrl = "url";
const columnScope = "scope";
const columnTags = "tags";
const columnAdId = "adId";
const createdAt = "_createdAt";
const databaseName = "sotex_box.db";

class BoxDatabase {
  static Database? database;

  Future<Database> get() async {
    if (database != null) {
      return database!;
    }

    database = await open(await _getDatabaseDirectory());
    if (database == null) {
      throw Exception("Unable to open the database");
    }

    return database!;
  }

  Future<void> clean() async {
    final db = await get();
    db.execute("DELETE FROM $tableScheduleItems");
  }

  Future<Database> open(String path) async {
    final db = await openDatabase(path, version: 1,
        onCreate: (Database db, int version) async {
      await db.execute('''
          create table if not exists $tableScheduleItems (
          $columnId INTEGER PRIMARY KEY AUTOINCREMENT,
          $createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,
          $columnUrl TEXT NOT NULL,
          $columnScope INTEGER NOT NULL,
          $columnTags TEXT NOT NULL,
          $columnAdId TEXT UNIQUE NOT NULL
          );
          ''');
    });
    return db;
  }

  Future<String> _getDatabaseDirectory() async {
    final appDirectory = await getApplicationSupportDirectory();
    final dbDirectory = join(appDirectory.path, databaseName);
    return dbDirectory;
  }
}
