class DeviceSchedule {
  final DateTime createdAt;
  final String deviceId;
  final List<ScheduleItem> schedule;

  DeviceSchedule({
    required this.createdAt,
    required this.deviceId,
    required this.schedule,
  });

  factory DeviceSchedule.fromJson(Map<String, dynamic> json) {
    return DeviceSchedule(
      createdAt: DateTime.parse(json['CreatedAt']),
      deviceId: json['DeviceId'],
      schedule: (json['Schedule'] as List)
          .map((item) => ScheduleItem.fromJson(item))
          .toList(),
    );
  }

  @override
  String toString() {
    return 'DeviceSchedule(createdAt: $createdAt, deviceId: $deviceId, schedule: $schedule)';
  }
}

class ScheduleItem {
  final String downloadLink;
  final Ad ad;

  ScheduleItem({
    required this.downloadLink,
    required this.ad,
  });

  factory ScheduleItem.fromJson(Map<String, dynamic> json) {
    return ScheduleItem(
      downloadLink: json['DownloadLink'],
      ad: Ad.fromJson(json['Ad']),
    );
  }

  @override
  String toString() {
    return 'ScheduleItem(downloadLink: $downloadLink, ad: $ad)';
  }
}

class Ad {
  final int scope;
  final List<String> tags;
  final String id;

  Ad({
    required this.scope,
    required this.tags,
    required this.id,
  });

  factory Ad.fromJson(Map<String, dynamic> json) {
    return Ad(
      scope: json['Scope'],
      tags: (json['Tags'] as List).cast<String>(),
      id: json['Id'],
    );
  }

  @override
  String toString() {
    return 'Ad(scope: $scope, tags: $tags, id: $id)';
  }
}
