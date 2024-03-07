import "package:launcher/logger.dart";
import "package:sse_channel/sse_channel.dart";

void readSSE() {
  final channel =
      SseChannel.connect(Uri.parse(const String.fromEnvironment("SERVER_URL")));

  channel.stream.listen((event) {
    logger.i("$event");
  });
}
