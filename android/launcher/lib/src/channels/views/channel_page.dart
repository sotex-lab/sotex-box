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

class ChannelPickerPageState extends State<ChannelPickerPage> {
  @override
  void dispose() {
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<PlaybackBloc, PlaybackState>(
      builder: (context, state) {
        if (state.current != null) {
          state.current!.initialize().then((value) => {
                state.current!.addListener(() {
                  logger.d("Current controller: ${state.current}");
                  if (state.current!.value.isCompleted) {
                    context.read<PlaybackBloc>().add(PlaybackPlayNext());
                  }
                })
              });
          state.current!.play();
          return AnimatedSwitcher(
              duration: const Duration(milliseconds: 200),
              transitionBuilder: (child, animation) {
                return FadeTransition(opacity: animation, child: child);
              },
              child: AspectRatio(
                aspectRatio: state.current!.value.aspectRatio,
                child: VideoPlayer(state.current!),
              ));
        } else {
          context.read<PlaybackBloc>().add(PlaybackPlayNext());
          return const Center(child: CircularProgressIndicator());
        }
      },
    );
  }
}
