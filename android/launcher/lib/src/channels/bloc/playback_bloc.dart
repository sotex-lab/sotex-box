import 'dart:collection';
import 'dart:io';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/database/media.dart';
import 'package:launcher/src/sse/models/schedule.dart';
import 'package:launcher/src/sse/providers/schedule_item_provider.dart';
import 'package:video_player/video_player.dart';

sealed class PlaybackEvent {}

final class PlaybackInitial extends PlaybackEvent {}

final class PlaybackPlayNext extends PlaybackEvent {}

// #####################

class PlaybackState {
  final Queue<ScheduleItem> playbackQueue;
  final VideoPlayerController? current;

  PlaybackState(this.playbackQueue, this.current);
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
      if (state.current != null) {
        state.current!.dispose();
      }
      VideoPlayerController? current;
      if (state.playbackQueue.isNotEmpty) {
        ScheduleItem item = state.playbackQueue.removeFirst();
        String? path = await getPathIfExistsForItem(item);

        if (path != null) {
          current = VideoPlayerController.file(File(path));
          emit(PlaybackState(state.playbackQueue, current));
        }
        return;
      } else {
        final queue =
            Queue<ScheduleItem>.from(await provider.getScheduleItems());
        if (queue.isNotEmpty) {
          ScheduleItem item = queue.removeFirst();
          String? path = await getPathIfExistsForItem(item);

          if (path != null) {
            current = VideoPlayerController.file(File(path));
          }
        }
        var newState = PlaybackState(queue, current);
        emit(newState);
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
