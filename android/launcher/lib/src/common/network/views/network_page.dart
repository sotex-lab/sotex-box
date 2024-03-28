import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/views/channel_page.dart';
import 'package:launcher/src/common/network/cubits/network_cubit.dart';

class WifiPickerPage extends StatelessWidget {
  const WifiPickerPage({super.key});

  void _navigateToHelloWorld(BuildContext context) {
    Navigator.of(context).push(
      MaterialPageRoute(builder: (context) => const ChannelPage()),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Wi-Fi Picker'),
      ),
      body: BlocListener<NetworkCubit, NetworkState>(
        listener: (context, state) {
          if (state == NetworkState.online) {
            _navigateToHelloWorld(context);
          }
        },
        child: Center(
          child: BlocBuilder<NetworkCubit, NetworkState>(
            builder: (context, state) {
              if (state == NetworkState.offline) {
                return const Text(
                    'No network connection. Please connect to Wi-Fi.');
              } else {
                return const CircularProgressIndicator();
              }
            },
          ),
        ),
      ),
    );
  }
}
