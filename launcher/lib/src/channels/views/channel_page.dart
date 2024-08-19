import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/bloc/playback_bloc.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:video_player/video_player.dart';

class ChannelPage extends StatefulWidget {
  const ChannelPage({Key? key}) : super(key: key);

  @override
  ChannelPageState createState() => ChannelPageState();
}

class ChannelPageState extends State<ChannelPage>
    with TickerProviderStateMixin {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<PlaybackBloc, PlaybackState>(
      builder: (context, state) {
        if (state.current != null) {
          state.current!.addListener(() {
            if (state.current!.value.position ==
                state.current!.value.duration) {
              context.read<PlaybackBloc>().add(PlaybackPlayNext());
            }
          });
          state.current!.play();
          return AspectRatio(
            aspectRatio: state.current!.value.aspectRatio,
            child: VideoPlayer(state.current!),
          );
        } else {
          context.read<PlaybackBloc>().add(PlaybackPlayNext());
          if (const String.fromEnvironment("build") == "DEBUG") {
            return const DiagnosticsViewer();
          } else {
            return Center(
                key: UniqueKey(), child: const CircularProgressIndicator());
          }
        }
      },
    );
  }
}

class DiagnosticsViewer extends StatelessWidget {
  const DiagnosticsViewer({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: Column(
        children: [
          // Status Bar
          Container(
            color: Colors.grey[900],
            padding: const EdgeInsets.all(16.0),
            width: double.infinity,
            child: const Text(
              'Diagnostics',
              style: TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          // Main Content
          Expanded(
            child: BlocBuilder<DebugBloc, DebugState>(
              builder: (context, state) {
                return Container(
                  width: double.infinity,
                  color: Colors.white,
                  padding: const EdgeInsets.all(10.0),
                  child: Text(
                    state.logQueue.join("\n"),
                    style: const TextStyle(
                      color: Colors.black,
                      fontSize: 16,
                    ),
                    textAlign: TextAlign.justify,
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
