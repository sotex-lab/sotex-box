import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/bloc/playback_bloc.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:video_player/video_player.dart';

class ChannelPickerPage extends StatefulWidget {
  const ChannelPickerPage({Key? key}) : super(key: key);

  @override
  ChannelPickerPageState createState() => ChannelPickerPageState();
}

class ChannelPickerPageState extends State<ChannelPickerPage>
    with TickerProviderStateMixin {
  late AnimationController _animationController;

  @override
  void initState() {
    super.initState();
    _animationController = AnimationController(
        vsync: this, duration: const Duration(milliseconds: 500), value: 1.0);
  }

  @override
  void dispose() {
    super.dispose();
    _animationController.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
        opacity: _animationController,
        child: BlocBuilder<DebugBloc, DebugState>(
          builder: (context, state) {
            return Scaffold(
              backgroundColor: Colors.black,
              body: Center(
                child: BlocBuilder<DebugBloc, DebugState>(
                  builder: (context, state) {
                    return Container(
                      padding: const EdgeInsets.all(16.0),
                      child: Text(
                        state.logQueue.join("\n"),
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 16,
                        ),
                        textAlign: TextAlign.justify,
                      ),
                    );
                  },
                ),
              ),
            );
          },
        ));
  }
}
