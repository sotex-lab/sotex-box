import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/common/network/cubits/network_cubit.dart';

class ChannelPickerPage extends StatelessWidget {
  const ChannelPickerPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<NetworkCubit, NetworkState>(
        builder: (context, state) => Scaffold(
              appBar: AppBar(
                title: Text('Connected ${state.toString()}'),
              ),
              body: const Center(
                child: Text('Hello World'),
              ),
            ));
  }
}
