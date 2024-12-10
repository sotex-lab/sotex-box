import 'dart:async';
import 'dart:collection';
import 'dart:io';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/database/media.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:launcher/src/sse/providers/schedule_item_provider.dart';
import 'package:video_player/video_player.dart';
import 'package:launcher/src/common/debug_singleton.dart';
import 'package:launcher/src/common/logging.dart';

sealed class PlaybackEvent {}

final class PlaybackInitial extends PlaybackEvent {}

final class PlaybackPlayNext extends PlaybackEvent {}

// #####################

class PlaybackState {
  final Queue<ScheduleItem> playbackQueue;
  VideoPlayerController? current;

  PlaybackState(this.playbackQueue, this.current);

  @override
  String toString() {
    return current.toString();
  }
}

class PlaybackBloc extends Bloc<PlaybackEvent, PlaybackState> {
  final ScheduleItemProvider provider = ScheduleItemProvider();
  Timer? _timer;

  PlaybackBloc() : super(PlaybackState(Queue<ScheduleItem>(), null)) {
      on<PlaybackPlayNext>((event, emit) async {
        if (  state.playbackQueue.isEmpty) {
          final queue = Queue<ScheduleItem>.from(await provider.getScheduleItems());

          if (queue.isEmpty && _timer == null) {
            DebugSingleton().getDebugBloc.add(DebugPushEvent("Queue empty"));
            _timer = Timer.periodic(Duration(minutes: 1), (_){
              add(PlaybackPlayNext());
            });
            return;
          }

          if(queue.isEmpty) return;

          ScheduleItem item = queue.removeFirst();
          String? path = await getPathIfExistsForItem(item);

          if (path == null) {
            await Future.delayed(const Duration(seconds: 3));
            //To continue with new queue
            emit(PlaybackState(queue, state.current));
            return;
          }
          
          if (!File(path).existsSync()) {
            DebugSingleton().getDebugBloc.add(DebugPushEvent("Video file does not exist: $path"));
            add(PlaybackPlayNext());
            return;
          }

          await _initializeAndPlay(path);
          _timer?.cancel();
          _timer = null;

          emit(PlaybackState(queue, state.current));
        } else {
          ScheduleItem item = state.playbackQueue.removeFirst();
          String? path = await getPathIfExistsForItem(item);

          if (path == null) {
            await Future.delayed(const Duration(seconds: 3));
            add(PlaybackPlayNext());
            return;
          }

          if (!File(path).existsSync()) {
            DebugSingleton().getDebugBloc.add(DebugPushEvent("Video file does not exist: $path"));
            add(PlaybackPlayNext());
            return;
          }

          await _initializeAndPlay(path);

          emit(PlaybackState(state.playbackQueue, state.current));
        }
      }
    );
  }

  Future<void> _initializeAndPlay(String path) async {
    state.current = VideoPlayerController.file(File(path));
    try {
      await state.current!.initialize().timeout(Duration(seconds: 10)); 
      state.current!.addListener(_videoListener);
      state.current!.setLooping(false);
      state.current!.play();
    } catch (error) {
      DebugSingleton().getDebugBloc.add(DebugPushEvent("Error with video, $error"));
      add(PlaybackPlayNext());
    }
  }

  void _videoListener() {
    if (state.current!.value.isInitialized &&
        !state.current!.value.isPlaying &&
        state.current!.value.position >= state.current!.value.duration) {
      state.current!.removeListener(_videoListener);
      state.current!.dispose();
      add(PlaybackPlayNext());
    }
  }
}
