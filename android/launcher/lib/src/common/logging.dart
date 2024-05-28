import 'dart:collection';
import 'dart:convert';
import 'dart:io';

import 'package:external_path/external_path.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';
import 'package:launcher/src/common/debug_singleton.dart';
import 'package:launcher/src/database/storage.dart';
import 'package:logger/logger.dart';
import 'package:path_provider/path_provider.dart';

class CustomFilter extends LogFilter {
  @override
  bool shouldLog(LogEvent event) {
    return true;
  }
}

class LogLevelMapper {
  Map<String, Level> _getLogLevelMapper() {
    return const {
      "OFF": Level.off,
      "TRACE": Level.trace,
      "INFO": Level.info,
      "DEBUG": Level.debug,
      "ERROR": Level.error,
      "FATAL": Level.fatal,
      "WARNING": Level.warning,
    };
  }

  getLogLevel() {
    const level = String.fromEnvironment("log_level", defaultValue: "");
    final mapper = _getLogLevelMapper();
    if (mapper.containsKey(level)) {
      return mapper[level];
    }

    return Level.info;
  }
}

class LogManager {
  Logger? logger;

  Stream<String> get logFileStream async* {
    var file = await _getLogFile();
    var lines = <String>[];
    var bufferSize = 10;

    while (true) {
      await for (var line in file
          .openRead()
          .transform(utf8.decoder)
          .transform(const LineSplitter())) {
        lines.add(line);
        if (lines.length > bufferSize) {
          lines.removeAt(
              0); // Remove the oldest line if more than bufferSize lines exist
        }
        yield lines
            .join('\n'); // Emit the last 20 lines joined as a single string
      }

      await Future.delayed(const Duration(seconds: 5));
    }
  }

  Future<File> _getLogFile() async {
    Directory appDir = Directory(await DirectoryGetter().getLogDirectory());
    DateTime now = DateTime.now();
    String formattedDate = DateFormat('yyyy-MM-dd').format(now);

    String fileName = 'log_$formattedDate.txt';
    String filePath = '${appDir.path}/$fileName';

    DebugSingleton()
        .getDebugBloc
        .add(DebugPushEvent("Log file path: $filePath"));

    final file = File(filePath);

    if (!(await file.exists())) {
      return (await File(filePath).create());
    }

    return File(filePath);
  }

  Future<void> removeLogFile() async {
    final file = await _getLogFile();
    file.deleteSync();
  }

  Future<Logger> getOrCreateLogger() async {
    if (logger != null) {
      return logger!;
    }

    if (const String.fromEnvironment("log_type") == "FILE") {
      logger = Logger(
          filter: CustomFilter(),
          level: LogLevelMapper().getLogLevel(),
          output: FileOutput(file: await _getLogFile()),
          printer: SimplePrinter());
    } else {
      logger = Logger(
          filter: CustomFilter(),
          level: LogLevelMapper().getLogLevel(),
          output: ConsoleOutput(),
          printer: SimplePrinter());
    }

    return logger!;
  }
}

sealed class DebugEvent {}

final class DebugPushEvent extends DebugEvent {
  final String message;
  DebugPushEvent(this.message);
}

class DebugState {
  final int bufferSize;
  final Queue<String> logQueue;

  DebugState(this.logQueue, this.bufferSize);

  @override
  String toString() {
    return logQueue.toString();
  }
}

class DebugBloc extends Bloc<DebugEvent, DebugState> {
  DebugBloc() : super(DebugState(Queue<String>(), 10)) {
    on<DebugPushEvent>((event, emit) async {
      (await LogManager().getOrCreateLogger())
          .i("Debug Push Event with message ${event.message}");
      if (state.logQueue.length < state.bufferSize) {
        state.logQueue.add(event.message);
      } else {
        state.logQueue.removeFirst();
        state.logQueue.add(event.message);
      }

      (await LogManager().getOrCreateLogger())
          .i("Debug Push Event with queue ${state.logQueue}");

      emit(DebugState(state.logQueue, state.bufferSize));
    });
  }
}
