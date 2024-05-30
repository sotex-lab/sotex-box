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
        child: BlocBuilder<PlaybackBloc, PlaybackState>(
          builder: (context, state) {
            if (_animationController.status != AnimationStatus.completed) {
              Future.delayed(const Duration(milliseconds: 500), () {
                _animationController.forward();
              });
            }
            if (state.current != null) {
              state.current!.initialize().then((value) => {
                    state.current!.addListener(() {
                      void animationListener() {
                        if (_animationController.status ==
                            AnimationStatus.dismissed) {
                          state.current!.dispose();
                          context.read<PlaybackBloc>().add(PlaybackPlayNext());
                          _animationController
                              .removeListener(animationListener);
                        }
                      }

                      if (state.current!.value.isCompleted &&
                          _animationController.status ==
                              AnimationStatus.completed) {
                        _animationController.addListener(animationListener);
                        _animationController.reverse();
                      }
                    })
                  });
              Future.delayed(const Duration(milliseconds: 500), () {
                state.current!.play();
              });
              return AspectRatio(
                aspectRatio: state.current!.value.aspectRatio,
                child: VideoPlayer(state.current!),
              );
            } else {
              context.read<PlaybackBloc>().add(PlaybackPlayNext());
              if (const String.fromEnvironment("build") == "DEBUG") {
                return BlocBuilder<DebugBloc, DebugState>(
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
                );
              } else {
                return Center(
                    key: UniqueKey(), child: const CircularProgressIndicator());
              }
            }
          },
        ));
  }
}
