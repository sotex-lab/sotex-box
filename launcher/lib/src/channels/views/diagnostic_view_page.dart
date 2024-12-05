import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:launcher/src/navigation/cubits/navigation_cubit.dart';
import 'package:upgrader/upgrader.dart';
import 'package:launcher/src/common/logging.dart';

class DiagnosticsViewer extends StatelessWidget {
  const DiagnosticsViewer({super.key});

  @override
  Widget build(BuildContext context) {    
    bool _isControlPressed = false;
    return Scaffold(
      backgroundColor: Colors.white,
      body: UpgradeAlert(
        shouldPopScope: () => false,
        child: KeyboardListener(
          focusNode: FocusNode(),
          autofocus: true,
          onKeyEvent: (event) async {
            if (event is KeyDownEvent) {
              if (event.logicalKey == LogicalKeyboardKey.controlLeft ||
                  event.logicalKey == LogicalKeyboardKey.controlRight) {
                _isControlPressed = true;
              }

              if (_isControlPressed && event.logicalKey == LogicalKeyboardKey.keyD) {
                if(context.mounted) BlocProvider.of<NavigationCubit>(context).goToChannelPicker();
              }
            }

            if (event is KeyUpEvent) {
              if (event.logicalKey == LogicalKeyboardKey.controlLeft ||
                  event.logicalKey == LogicalKeyboardKey.controlRight) {
                _isControlPressed = false;
              }
            }
          },
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
      ),
    );
  }
}