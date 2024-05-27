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
              if (const String.fromEnvironment("build") == "DEBUG" &&
                  const String.fromEnvironment("log_type") == "FILE") {
                return StreamBuilder(
                    stream: LogManager().logFileStream,
                    builder: (context, snapshot) {
                      return Scaffold(
                        body: Center(
                          child: Padding(
                            padding: const EdgeInsets.all(20.0),
                            child: Text(
                              snapshot.data ??
                                  'No data available', // Display 'No data available' if log is null
                              textAlign: TextAlign.center,
                              style: const TextStyle(
                                fontSize: 16.0,
                                fontWeight: FontWeight.normal,
                                color: Colors.white,
                              ),
                            ),
                          ),
                        ),
                      );
                    });
              } else {
                return Center(
                    key: UniqueKey(), child: const CircularProgressIndicator());
              }
            }
          },
        ));
  }
}
