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
  final VideoPlayerController? current;

  PlaybackState(this.playbackQueue, this.current);

  @override
  String toString() {
    return current.toString();
  }
}

class PlaybackBloc extends Bloc<PlaybackEvent, PlaybackState> {
  final ScheduleItemProvider provider = ScheduleItemProvider();

  PlaybackBloc() : super(PlaybackState(Queue<ScheduleItem>(), null)) {
    on<PlaybackInitial>((event, emit) async {
      VideoPlayerController? current;
      final queue = Queue<ScheduleItem>.from(await provider.getScheduleItems());
      if (queue.isNotEmpty) {
        ScheduleItem item = queue.removeFirst();
        String? path = await getPathIfExistsForItem(item);

        if (path != null) {
          current = VideoPlayerController.file(File(path));
        }
      }
      var newState = PlaybackState(queue, current);
      emit(newState);
    });

    on<PlaybackPlayNext>((event, emit) async {
      VideoPlayerController? playerController;

      while (true) {
        if (state.playbackQueue.isEmpty) {
          final queue =
              Queue<ScheduleItem>.from(await provider.getScheduleItems());
          if (queue.isEmpty) {
            DebugSingleton().getDebugBloc.add(DebugPushEvent("Queue empty"));
            await Future.delayed(const Duration(seconds: 5));
            continue;
          }

          ScheduleItem item = queue.removeFirst();
          DebugSingleton()
              .getDebugBloc
              .add(DebugPushEvent("Schedule item: ${item.ad.id}"));
          String? path = await getPathIfExistsForItem(item);

          if (path == null) {
            await Future.delayed(const Duration(seconds: 5));
            continue;
          }

          playerController = VideoPlayerController.file(File(path));
          var newState = PlaybackState(queue, playerController);
          emit(newState);
          return;
        }

        ScheduleItem item = state.playbackQueue.removeFirst();
        String? path = await getPathIfExistsForItem(item);
        if (path == null) continue;
        playerController = VideoPlayerController.file(File(path));
        emit(PlaybackState(state.playbackQueue, playerController));
        return;
      }
    });
  }

  @override
  void onChange(Change<PlaybackState> change) {
    super.onChange(change);
  }

  @override
  void onTransition(Transition<PlaybackEvent, PlaybackState> transition) {
    super.onTransition(transition);
  }

  @override
  void onError(Object error, StackTrace stackTrace) {
    super.onError(error, stackTrace);
  }
}
