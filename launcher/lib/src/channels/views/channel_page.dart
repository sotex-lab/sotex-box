import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/bloc/playback_bloc.dart';
import 'package:launcher/src/common/logging.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:upgrader/upgrader.dart';
import 'package:video_player/video_player.dart';

class ChannelPage extends StatefulWidget {
  const ChannelPage({super.key});

  @override
  ChannelPageState createState() => ChannelPageState();
}

class ChannelPageState extends State<ChannelPage>
    with TickerProviderStateMixin {
  @override
  void initState() {
    super.initState();
  }

  bool _isControlPressed = false;

  @override
  Widget build(BuildContext context) {
    return KeyboardListener(
      focusNode: FocusNode(),
      autofocus: true,
      onKeyEvent: (event) async {
        if (event is KeyDownEvent) {
          if (event.logicalKey == LogicalKeyboardKey.controlLeft ||
              event.logicalKey == LogicalKeyboardKey.controlRight) {
            _isControlPressed = true;
          }

          if (_isControlPressed && event.logicalKey == LogicalKeyboardKey.keyR) {
            final prefs = await SharedPreferences.getInstance();
            await prefs.remove('deviceId');
            if(context.mounted) BlocProvider.of<NavigationCubit>(context).goToDeviceRegistration();
          }
        }

        if (event is KeyUpEvent) {
          if (event.logicalKey == LogicalKeyboardKey.controlLeft ||
              event.logicalKey == LogicalKeyboardKey.controlRight) {
            _isControlPressed = false;
          }
        }
      },
      child: BlocBuilder<PlaybackBloc, PlaybackState>(
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
      ),
    );
  }
}

class DiagnosticsViewer extends StatelessWidget {
  const DiagnosticsViewer({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: UpgradeAlert(
        shouldPopScope: () => false,
        child: Column(
          children: [
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
      ),
    );
  }
}
