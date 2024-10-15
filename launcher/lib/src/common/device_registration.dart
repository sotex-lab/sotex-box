import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:launcher/src/sse/sse_entry.dart';
import 'package:shared_preferences/shared_preferences.dart';

class DeviceRegistrationPage extends StatefulWidget {
  const DeviceRegistrationPage({super.key});

  @override
  State<DeviceRegistrationPage> createState() => _DeviceRegistrationPageState();
}

class _DeviceRegistrationPageState extends State<DeviceRegistrationPage> {
  
  String? deviceId;
  final FocusNode _focusNode = FocusNode();

  @override
  void initState() {
    super.initState();

    WidgetsBinding.instance.addPostFrameCallback((_) {
      _focusNode.requestFocus();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Enter Device ID'),
      ),
      
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            TextField(
              focusNode: _focusNode,
              onChanged: (value) {
                deviceId = value;
              },
              onSubmitted: (String value) async {
                if(deviceId == null) return;
                final prefs = await SharedPreferences.getInstance();
                await prefs.setString('deviceId', deviceId!);
              },
              decoration: const InputDecoration(hintText: "Device ID"),
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: () async{
                BlocProvider.of<NavigationCubit>(context).goToChannelPicker();
                final prefs = await SharedPreferences.getInstance();
                await prefs.setString('deviceId', deviceId ?? '');
                if(context.mounted) startListeningForSSE(context, deviceId: deviceId);
              },
              child: const Text('Start Listening'),
            ),
          ],
        ),
      ),
    );
  }
}