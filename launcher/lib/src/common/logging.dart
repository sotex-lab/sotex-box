import 'dart:collection';
import 'dart:io';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';
import 'package:launcher/src/common/debug_singleton.dart';
import 'package:launcher/src/database/storage.dart';
import 'package:logger/logger.dart';

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
  Logger? consoleLogger;
  Logger? fileSystemLogger;

  LogManager._privateConstructor();

  static final LogManager _instance = LogManager._privateConstructor();

  factory LogManager() {
    return _instance;
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

  Future<void> ensureCreated() async {
    fileSystemLogger ??= Logger(
        filter: CustomFilter(),
        level: LogLevelMapper().getLogLevel(),
        output: FileOutput(file: await _getLogFile()),
        printer: SimplePrinter());

    consoleLogger ??= Logger(
        filter: CustomFilter(),
        level: LogLevelMapper().getLogLevel(),
        output: ConsoleOutput(),
        printer: SimplePrinter());
  }

  void i(String message) {
    if (consoleLogger != null) {
      consoleLogger!.i(message);
    }

    if (fileSystemLogger != null) {
      fileSystemLogger!.i(message);
    }
  }

  void w(String message) {
    if (consoleLogger != null) {
      consoleLogger!.w(message);
    }

    if (fileSystemLogger != null) {
      fileSystemLogger!.w(message);
    }
  }

  void e(String message) {
    if (consoleLogger != null) {
      consoleLogger!.e(message);
    }

    if (fileSystemLogger != null) {
      fileSystemLogger!.e(message);
    }
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
  DebugBloc() : super(DebugState(Queue<String>(), 25)) {
    on<DebugPushEvent>((event, emit) async {
      if (state.logQueue.length < state.bufferSize) {
        state.logQueue.add(event.message);
      } else {
        state.logQueue.removeFirst();
        state.logQueue.add(event.message);
      }
      emit(DebugState(state.logQueue, state.bufferSize));
    });
  }
}
