import 'package:logger/logger.dart';

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

class CustomFilter extends LogFilter {
  @override
  bool shouldLog(LogEvent event) {
    return true;
  }
}

final logger = Logger(
    filter: CustomFilter(),
    level: LogLevelMapper().getLogLevel(),
    output: ConsoleOutput(),
    printer: SimplePrinter());
