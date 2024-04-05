import 'package:logger/logger.dart';

class CustomFilter extends LogFilter {
  @override
  bool shouldLog(LogEvent event) {
    return true;
  }
}

final logger = Logger(
    filter: CustomFilter(),
    level: Level.all,
    output: ConsoleOutput(),
    printer: SimplePrinter());
