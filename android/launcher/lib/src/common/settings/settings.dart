import 'package:url_launcher/url_launcher.dart';

final Uri uri = Uri.parse("package:com.android.settings");
final Uri fallback = Uri.parse("package:com.android.tv.settings");

Future<void> openSettings() async {
  try {
    if (!await launchUrl(uri)) {
      throw "Nije moguće otvoriti Wi-Fi podešavanja.";
    }
  } catch (e) {
    if (!await launchUrl(fallback)) {
      throw "Nije moguće otvoriti Wi-Fi podešavanja.";
    }
  }
}
