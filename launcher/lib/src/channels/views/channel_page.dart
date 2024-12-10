import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/channels/bloc/playback_bloc.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:shared_preferences/shared_preferences.dart';
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
  late VideoPlayer videoPlayer;
  late double aspectRatio;

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

          if (_isControlPressed && event.logicalKey == LogicalKeyboardKey.keyD) {
            if(context.mounted) BlocProvider.of<NavigationCubit>(context).goToDiagnosticView();
          }
        }

        if (event is KeyUpEvent) {
          if (event.logicalKey == LogicalKeyboardKey.controlLeft ||
              event.logicalKey == LogicalKeyboardKey.controlRight) {
            _isControlPressed = false;
          }
        }
      },
      child: SafeArea(
        child: BlocBuilder<PlaybackBloc, PlaybackState>(
          builder: (context, state) {
            if(state.current != null) {
              videoPlayer = VideoPlayer(state.current!);
              aspectRatio = state.current!.value.aspectRatio;

              bool isVerticalVideo = aspectRatio < 0.75;

              return Stack(
                fit: StackFit.expand,
                children: [
                  if (isVerticalVideo) ...[
                    Positioned.fill(
                      child: Transform.scale(
                        scale: 1.5,
                        child: ColorFiltered(
                          colorFilter: ColorFilter.mode(
                            Colors.black.withOpacity(0.3),
                            BlendMode.darken,
                          ),
                          child: VideoPlayer(state.current!),
                        ),
                      ),
                    ),
                    Positioned.fill(
                      child: BackdropFilter(
                        filter: ImageFilter.blur(sigmaX: 15.0, sigmaY: 15.0),
                        child: Container(
                          color: Colors.black.withOpacity(0.5), 
                        ),
                      ),
                    ),
                  ],
                  Center(
                    child: AspectRatio(
                      aspectRatio: aspectRatio,
                      child: videoPlayer,
                    ),
                  ),
                ],
              );
            } else {
              context.read<PlaybackBloc>().add(PlaybackPlayNext());
              return Center(child: CircularProgressIndicator());
            }
          },
        ),
      ),
    );
  }
}
