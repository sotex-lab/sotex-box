import 'package:flutter_bloc/flutter_bloc.dart';

abstract class NavigationState {}

class WiFiPicker extends NavigationState {}

class ChannelPicker extends NavigationState {}

class DeviceRegistration extends NavigationState {}

class Diagnostics extends NavigationState {}

class NavigationCubit extends Cubit<NavigationState> {
  NavigationCubit() : super(WiFiPicker());

  void goToWiFiPicker() => emit(WiFiPicker());

  void goToChannelPicker() => emit(ChannelPicker());

  void goToDeviceRegistration() => emit(DeviceRegistration());

  void goToDiagnosticView() => emit(Diagnostics());
}
