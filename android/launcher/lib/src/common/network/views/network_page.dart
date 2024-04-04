import 'package:app_settings/app_settings.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/common/network/cubits/network_cubit.dart';

class WifiPickerPage extends StatelessWidget {
  const WifiPickerPage({super.key});

  void _navigateToChannelPage(BuildContext context) {
    Navigator.of(context).push(
      MaterialPageRoute(builder: (context) => const ChannelPickerPage()),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        appBar: AppBar(
          title: SizedBox(
            height: kToolbarHeight - 8.0,
            child: Image.asset(
              'assets/images/sotex_solutions.png',
              fit: BoxFit.contain,
            ),
          ),
        ),
        body: Center(
            child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            const Text(
              "Ne postoji konekcija ka internetu. Molimo Vas da se povežete na Wi-Fi.",
              style: TextStyle(fontSize: 32),
            ),
            const SizedBox(
              height: 28,
            ),
            ElevatedButton(
                onPressed: () {
                  AppSettings.openAppSettings(type: AppSettingsType.wifi);
                },
                child: const Padding(
                    padding: EdgeInsets.all(16.0),
                    child: Text(
                      "Wi-Fi podešavanja.",
                      style: TextStyle(fontSize: 32),
                    )))
          ],
        )));
  }
}
