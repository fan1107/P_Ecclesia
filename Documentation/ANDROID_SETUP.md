# 에끌레시아 Android 시제품 설정

이 작업의 변경 대상은 `P_Ecclesia/P_Ecclesia`뿐입니다. `pixel_war_ship`은 수정하거나 빌드하지 않습니다.

## 프로젝트 열기

- Unity Hub에서 `ProjectSettings/ProjectVersion.txt`에 지정된 에디터 버전과 Android Build Support, SDK/NDK, OpenJDK를 설치합니다.
- 메뉴 `Ecclesia > 1. Prepare Android and Scene`으로 설정 및 시작 씬을 준비합니다.
- `Ecclesia > 3. Open Prototype`으로 씬을 열고 Play를 누릅니다.
- `오프라인 둘러보기` → 교회 문 터치 → 장의자 좌석 터치 → `기도 시작`.
- 바닥을 터치하면 기도를 마치고 이동합니다. 예배당 아래 문으로 뜰에 나갑니다.
- 현재 화면과 발소리는 코드로 만든 기능 검증용 임시 도트 아트/합성 음원입니다.
- 현재 범위는 ① Android 기반, ② 로그인 연동 코드, ③ 오프라인 공간입니다. 온라인 접속자·기도 공개는 아직 없습니다.

## 이미 추가된 의존성

- Firebase Authentication과 Google Analytics for Firebase Unity SDK **14.1.0**, 함께 제공되는 External Dependency Manager.
- Google 공식 `googlesamples/siwg-mobile-unity-plugin` 소스의 Android 플러그인과 C# 런타임.
- Noto Sans KR (SIL Open Font License). 라이선스는 폰트와 함께 포함했습니다.
- Google 플러그인 문서와 실제 코드의 이벤트 이름이 달라 실제 코드의 `OnSignInSuccess`, `OnSignInError`에 맞췄습니다.
- Google 플러그인의 AndroidManifest.xml에서 앞선 주석 뒤에 있던 XML 선언을 제거했습니다.
- Android 플러그인의 초기화 실패 콜백 이름을 실제 메서드에 맞게 수정하고, 실행 예외도 UI로 전달하도록 보완했습니다.
- Unity 6000.3.28의 AGP 9 빌드에서 Google 플러그인의 Kotlin 소스가 컴파일되도록 built-in Kotlin을 활성화하고 Android 라이브러리의 manifest 경로를 지정합니다. 구버전 Kotlin Gradle 템플릿을 추가하지 않습니다.

## Google Analytics 연결

1. [Firebase 콘솔](https://console.firebase.google.com/)에서 이 앱의 프로젝트를 선택합니다. 프로젝트 설정의 **통합 > Google 애널리틱스**에서 GA4 속성을 연결합니다.
2. 같은 프로젝트의 **프로젝트 설정 > 일반 > 내 앱**에서 패키지 이름이 `com.ecclesia.prayer`인 Android 앱을 선택합니다. 처음이면 이 패키지 이름으로 Android 앱을 등록합니다.
3. 해당 앱의 `google-services.json`을 내려받아 Unity 프로젝트의 `Assets/google-services.json`에 넣고 Android 앱을 다시 빌드합니다. 빌드 전처리가 패키지 이름과 Firebase 앱 ID를 확인합니다. Unity에서 미리 확인하려면 **Ecclesia > 2. Configure Firebase from google-services.json**을 실행합니다.
4. 설정된 Android 앱을 실행하면 첫 씬 전에 Firebase 의존성을 확인한 뒤 Analytics 수집을 활성화합니다. Google 로그인 없이 SDK 기본 이벤트를 수집합니다. 설정 파일이 없으면 오프라인으로 실행할 수 있습니다.

### Android 실기기에서 수신 확인

USB 디버깅을 허용한 기기에 앱을 설치하고 아래 명령을 실행한 뒤 앱을 다시 엽니다. Firebase 콘솔의 **애널리틱스 > DebugView**에서 해당 기기의 `session_start` 이벤트를 확인합니다. [Firebase DebugView 안내](https://firebase.google.com/docs/analytics/debugview)

```sh
adb shell setprop debug.firebase.analytics.app com.ecclesia.prayer
```

검증이 끝나면 `adb shell setprop debug.firebase.analytics.app .none.`으로 디버그 모드를 해제합니다.

## Google 로그인도 사용할 때

1. [Firebase 콘솔](https://console.firebase.google.com/)에서 위의 Analytics 연결에 사용한 프로젝트를 엽니다.
2. 등록한 Android 앱의 패키지명이 **`com.ecclesia.prayer`**인지 확인합니다.
3. Firebase Authentication에서 Google 로그인 제공업체를 활성화하고 지원 이메일을 선택합니다.
4. 프로젝트 설정의 Android 앱에 빌드 서명 인증서의 **SHA-1 및 SHA-256**을 등록합니다.
   - 개발 APK: 이 PC의 Android debug keystore 인증서.
   - Play 내부 테스트: Google Play Console의 **앱 서명 인증서**도 등록합니다. 업로드 키와 다를 수 있습니다.
   - 키 파일이나 비밀번호를 채팅으로 보낼 필요는 없습니다.
   - 이 PC의 개발 인증서 지문은 `Documentation/DEBUG_CERTIFICATE.txt`에 추출했습니다.
5. Google 설정을 활성화한 후 최신 `google-services.json`을 다운로드합니다.
6. 파일을 이 프로젝트의 **`Assets/google-services.json`**에 둡니다. Git에서는 제외되어 있습니다.
7. Unity 메뉴 **`Ecclesia > 2. Configure Firebase from google-services.json`**을 실행합니다. 패키지 일치 여부를 확인하고 Web OAuth Client ID(type 3)를 Google 설정에 자동 반영합니다.
8. Web Client ID가 없으면 Analytics만 초기화됩니다. Google 로그인을 사용하려면 Firebase의 Google 제공업체 활성화 여부를 확인하고 JSON을 다시 받습니다.
9. OAuth 앱이 테스트 사용자 제한 상태이면 본인의 테스트 Google 계정을 허용 목록에 추가합니다.

설정 확인 마커는 구성 파일의 존재를 나타낼 뿐, 인증 성공을 증명하지 않습니다. 실제 로그인은 Android 실기기에서 검증해야 합니다. ID 토큰은 파일이나 PlayerPrefs에 저장하지 않으며, 로그에도 출력하지 않습니다. 세션 복원은 Firebase SDK에 맡깁니다.

## 빌드 및 설치

- 가로 화면, ARM64, IL2CPP, Minimum API 34 / Target API 36으로 시작합니다. 현재 공식 Google Unity 로그인 안내의 최소 API 요구사항을 따릅니다.
- `File > Build Profiles`에서 Android로 전환합니다.
- `Assets > External Dependency Manager > Android Resolver > Force Resolve`로 의존성을 해결합니다.
- `Ecclesia > 4. Build Android APK` 실행. 출력: `Builds/Android/Ecclesia-prototype.apk`.
- Android 기기의 USB 디버깅을 켜고 PC 연결을 직접 허용한 뒤 설치/실행합니다.
- 에디터에서는 Google 로그인 버튼이 활성화되지 않습니다. Firebase 설정 전에도 오프라인 공간은 실행됩니다.
- 이 APK는 개발용입니다. 배포용 키, 앱 아이콘, 스토어 등록, 계정 삭제 및 개인정보 안내는 정식 배포 전에 별도로 완성해야 합니다.

## 실기기 확인 체크리스트

- 최초 Google 로그인 → Firebase Authentication 사용자 목록에 계정 생성.
- 앱 재실행 → 로그인 상태 복원.
- 로그인 취소·네트워크 오류 → 오류 표시 후 재시도 가능.
- 뜰에서 문까지 이동, 예배당 전환, 좌석 도착.
- 의자 등받이/벽을 관통하지 않는지 확인.
- 이동 중 발소리, 실내 잔향, 기도 시작/종료와 빛 확인.
- 한글 기도제목 입력, 300자 제한, 입력창 밖 터치로 잘못 이동하지 않는지 확인.
- 백그라운드 전환 시 기도 종료, 복귀 시 입구에서 다시 시작.
- 노치 기기에서 버튼이 잘리지 않는지 확인.

## 근거 문서

- https://firebase.google.com/docs/auth/unity/google-signin
- https://firebase.google.com/docs/unity/setup
- https://github.com/googlesamples/siwg-mobile-unity-plugin

로그인 설정과 실기기 검증이 끝나면 다음 단계에서 Realtime Database 및 좌석 서버 처리를 연결합니다.
