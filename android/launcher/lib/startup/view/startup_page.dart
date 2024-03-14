import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/network/cubits/network_cubit.dart';

class StartupPage extends StatelessWidget {
  const StartupPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => NetworkCubit(),
      child: MaterialApp(
        home: Center(
          child: BlocBuilder<NetworkCubit, NetworkState>(
            builder: (context, state) {
              if (state == NetworkState.online) {
                return const Text("You are online");
              } else {
                // WidgetsBinding.instance.addPostFrameCallback((_) {
                //   Navigator.push(
                //       context,
                //       MaterialPageRoute(
                //           builder: (context) => const Text("Hello World")));
                // });
                return const Text("You are offline");
              }
            },
          ),
        ),
      ),
    );
  }
}
