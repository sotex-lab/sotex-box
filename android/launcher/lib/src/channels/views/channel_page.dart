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
        logger.d("Current path: ${state.current}");
        if (state.current != null) {
          return AspectRatio(
            aspectRatio: state.current!.value.aspectRatio,
            child: VideoPlayer(state.current!),
          );
        } else {
          context.read<PlaybackBloc>().add(PlaybackPlayNext());
          return const Center(child: CircularProgressIndicator());
        }
      },
    );
  }
}
