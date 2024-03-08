import 'dart:isolate';

class IsolateWorker {
  late Uri uri;

  IsolateWorker(Uri uri) {
    uri = uri;
  }

  Future<Isolate> spawn(
      dynamic isolateFunction, dynamic? handleResponseFunction) async {
    if (handleResponseFunction == null) {
      return await Isolate.spawn(isolateFunction, null);
    }
    final receivePort = ReceivePort();
    receivePort.listen(handleResponseFunction);
    return await Isolate.spawn(isolateFunction, receivePort.sendPort);
  }
}
