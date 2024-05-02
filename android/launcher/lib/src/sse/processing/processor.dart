import 'package:launcher/src/sse/sse_entry.dart';

abstract class Processor {
  final SSE message;

  Processor(this.message);

  Future<bool> process() async {
    return true;
  }
}
