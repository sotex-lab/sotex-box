import 'package:http/http.dart';
import 'package:launcher/common.dart';
import 'package:launcher/logger.dart';
import 'package:rxdart/rxdart.dart';

void listenForSSE(dynamic uri) async {
  if (uri is! Uri) {
    return;
  }

  Request request = Request("GET", uri);
  var stream = await getStream(request);

  await for (var sse in stream) {
    logger.i("SSE : $sse");
  }
}

Future<ByteStream> getStream(Request request) async {
  final client = Client();
  StreamedResponse response = await client.send(request);
  return response.stream;
}
