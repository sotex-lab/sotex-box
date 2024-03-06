import 'package:bloc/bloc.dart';
import 'package:flutter/widgets.dart';
import 'package:launcher/app.dart';
import 'package:launcher/app_observer.dart';

void main() async {
  Bloc.observer = const AppObserver();
  runApp(const SotexBoxApp());
}
