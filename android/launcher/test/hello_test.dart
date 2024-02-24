import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Simple Hello World Test', () {
    test('Check if Hello World string is correct', () {
      var helloWorldString = 'Hello, World!';

      expect(helloWorldString, 'Hello, World!');
    });
  });
}
