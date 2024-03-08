import 'package:bloc/bloc.dart';
import 'package:flutter/widgets.dart';
import 'package:launcher/app.dart';
import 'package:launcher/app_observer.dart';
import 'package:launcher/common.dart';
import 'package:launcher/isolate_worker.dart';
import 'package:launcher/sse/lib/sse_listener.dart';

void main() async {
  Bloc.observer = const AppObserver();
  IsolateWorker worker = IsolateWorker(Uri.parse(serverHost + path));
  await worker.spawn(listenForSSE, null);
  runApp(const SotexBoxApp());
}
