import 'package:launcher/logging.dart';

int tryCastInt(String val, {required int fallback}) {
  try {
    return int.parse(val);
  } catch (e) {
    logger.e('CastError when trying to cast $val to int!');
    return fallback;
  }
}
