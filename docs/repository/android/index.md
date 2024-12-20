We use [Dart](https://dart.dev/guides) and [Flutter](https://docs.flutter.dev/) for the device frontend.

## High level frontend overview

Two [Flutter](https://docs.flutter.dev/) applications support this frontend.
Launcher is going to be our environment UI which will take care of the things like splashscreen, device setup, device settings, and opening the Sotex Box app.
Sotex Box is an actual app which will provide the targeted media services.

## Emulation

You are free to use either software emulation or android supported hardware. To setup the local software emulator you can use make:
```bash
make setup-emulator
```
**You only have to do this once**, but for the sake of simplicity and possibility of platform version changes it is added as a command.

Before running any other part of the frontend you have to spin up the emulator.
```bash
make run-emu
```

Emulator might not be in the optimal orientation, so you can rotate it using:
```bash
make rotate-emu
```

Emulator runs in 1080p resolution however to have it in full-screen you can extend the window
by dragging out the upper left corner.

Emulator might not support back and home buttons. To remedy these go to: ```~/.android/avd/android_tv.avd/config.ini```.

Set ```hw.dpad = yes```
Set ```hw.keyboard = yes```

In order to run a particular part of the frontend you can use make:

```bash
make run-launcher
```

## Testing

There is a heavy emphasis on both unit and integration testing. This doesn't mean we follow [TDD](https://en.wikipedia.org/wiki/Test-driven_development) but we just want to ensure the code going to an environment does what it is supposed to. On top of that each bug or incident we receive we will create a test for it making sure that the same bug doesn't repeat itself.

Sometimes when writing tests developers tend to adhere to [DRY](https://en.wikipedia.org/wiki/Don't_repeat_yourself) principles too much. Try to aim for maintainable and readable tests rather than adhere to a principle.

To run all the tests you can use `make`:
```bash
make flutter-test
```

To run a particular set of tests you can use `make`:
```bash
make flutter-test-launcher
```

If you have problems setting it up, look at this [troubleshooting guide](troubleshoot.md).
