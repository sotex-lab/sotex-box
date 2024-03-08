import 'package:http/http.dart';

const serverHost =
    String.fromEnvironment("SERVER_URL", defaultValue: "http://10.0.2.2:8080");
const path = "/event/connect?id=device_1";
