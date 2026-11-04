# 에끌레시아 첫 개발 묶음

## 구성

`Assets/Ecclesia/Scenes/Ecclesia.unity`는 EcclesiaApp이 연결된 시작 씬입니다. 실행 시 공간, 캐릭터, UI를 생성합니다. 기존 SampleScene은 보존했습니다.

| 파일 | 책임 |
| --- | --- |
| Runtime/EcclesiaApp.cs | 화면 흐름, 터치 입력, 좌석 및 오프라인 기도 상태 |
| Runtime/ChapelWorld.cs | 뜰/예배당 Tilemap, 20석, 문·좌석 터치 영역 |
| Runtime/WalkGrid.cs | 네 방향 BFS 경로 탐색과 벽·등받이 이동 제한 |
| Runtime/Visitor.cs | 이동, 걷기 흔들림, 합성 발소리·잔향, 기도 빛 |
| Runtime/AuthenticationService.cs | Firebase 초기화, Google 토큰 교환, 세션 확인·로그아웃 |
| Editor/ProjectSetup.cs | Android 설정, 씬 생성, Firebase JSON 검증·설정, APK 빌드 |
| Editor/AndroidDependencies.cs | SDK XML에 명시된 버전으로 Gradle 의존성과 로컬 Firebase Maven 저장소 추가 |
| Editor/RuntimeValidation.cs | 플레이 흐름 자동 검증 및 화면 캡처 |

## 현재 경계

- Google 로그인에 필요한 SDK와 어댑터는 포함되어 있습니다. Firebase 프로젝트 설정 파일 및 실기기 인증 검증은 별도로 필요합니다.
- 오프라인 모드는 서버 인증 성공으로 처리하지 않습니다. 실제 Firebase UID를 만들거나 가짜 접속자를 표시하지 않습니다.
- 기도제목은 메모리에서만 사용하고 파일·PlayerPrefs·서버에 저장하지 않습니다.
- 자리 점유는 로컬 상태입니다. 이후 서버 단계에서 authoritative seat claim 결과에 연결해야 합니다.
- 온라인 데이터베이스, 접속 복구, 다른 사람의 기도 조회는 다음 개발 묶음입니다.
- 현재 기본 패키지 ID는 `com.ecclesia.prayer`입니다.
- 현재 Google 플러그인과 Firebase 14.1.0의 Android 지원 기준에 맞춰 최소 Android 14(API 34)로 시작합니다. 더 오래된 기기 지원은 의존성 호환성을 검토한 뒤 조정해야 합니다.

## 검증 방법

Unity 배치 모드에서 `Ecclesia.EditorTools.ProjectSetup.Validate`는 모든 좌석 접근·퇴장, 장애물 거부, 터치 영역을 검증합니다.

`Ecclesia.EditorTools.RuntimeValidation.Run`은 그래픽이 있는 배치 모드에서 실행합니다(`-nographics` 사용 금지, `-quit` 사용 금지). 검증 완료 시 직접 종료합니다. 로그인/뜰/기도 장면 PNG와 결과를 `Builds/Validation`에 저장합니다.

에디터가 열린 원본 프로젝트를 동시에 배치 실행하면 잠금 충돌이 나므로, 이번 작업에서는 프로젝트 안의 `Tools/ValidationProject` 사본을 사용했습니다. 이 사본은 Git에서 제외됩니다. 외부 프로젝트를 빌드하거나 수정하지 않습니다.

빌드 방법과 사용자가 직접 해야 하는 설정은 `ANDROID_SETUP.md`를 참고하세요.
