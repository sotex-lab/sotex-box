We use [Dart](https://dart.dev/guides) and [Flutter](https://docs.flutter.dev/) for the device frontend.

## High level frontend overview

Two [Flutter](https://docs.flutter.dev/) applications support this frontend.
Launcher is going to be our environment UI which will take care of the things like splashscreen, device setup, device settings, and opening the Sotex Box app.
Sotex Box is an actual app which will provide the targeted media services.

## Setup

Install Flutter
```bash
sudo snap install flutter --classic
```
Check if installation went well using:
```bash
flutter doctor
```
It will tell you that you are missing Android Studio and cmdline-tools (sdkmanager).
Download Android Studio from the official link.
Extract it:
```bash
tar -xzf place-android-studio-zip-here.tar.gz
```
cd into the newly created folder and run this command:
```bash
./bin bash studio.sh
```
This will run Android Studio installer. Use the recommended installation. Once it is finished, open it and create a sample project. When configuring a sample project make sure to select
Android 10 SDK. Let the project initialize completely.
Download cmdline-tools.
Extract it in ~/Android/Sdk/:
```bash
tar -xzf cmdline-tools.tar.gz -C ~/Android/Sdk
```
Move the contents of the newly created folder should be placed into the latest directory.
```bash
mv ~/Android/Sdk/cmdline-tools/** ~/Android/Sdk/cmdline-tools/latest/
```
You can add the ~/Android/Sdk/cmdline-tools/latest/bin to PATH to have easy access to tools.
```bash
echo 'export PATH="$HOME/Android/Sdk/cmdline-tools/latest/bin:$PATH"' >> ~/.bashrc
```
Go to bin directory, find sdkmanager and do.

```bash
./sdkmanager --install "cmdline-tools;latest"
```

You can run flutter doctor again and see that is ready to go.


## Emulation

You are free to use either software emulation or android supported hardware. To setup the local software emulator you can use make:
```bash
make flutter-create-emulator
```
**You only have to do this once**, but for the sake of simplicity and possibility of platform version changes it is added as a command.

In order to run a particular part of the frontend you can use make:

```bash
make flutter-run-launcher # or
make flutter-run-box
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
make flutter-test-launcher # or
make flutter-test-box
```
