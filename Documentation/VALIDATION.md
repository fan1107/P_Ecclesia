# 검증 결과 — 2026-11-04

## 완료

- Unity 6000.3.28f1에서 C# 컴파일 완료.
- 뜰 입구 접근, 예배당 20개 좌석 접근·퇴장 경로, 등받이·지도 밖 이동 거부, 문·좌석 터치 영역 검증 통과.
- 실제 Play 모드에서 오프라인 입장 → 뜰 이동 → 예배당 전환 → 좌석 도착 → 기도 빛 → 이동 시 기도 종료 → 뜰 퇴장 흐름 통과.
- 한글 시작 화면과 기도 화면을 렌더링하여 확인. 캡처: `Builds/Validation`.
- 검증 사본과 원본의 Ecclesia C# 파일 내용 일치 확인.
- 원본 시작 씬의 MonoBehaviour GUID와 Build Settings의 씬 GUID 연결 확인.
- Android 개발 APK 빌드 성공: **Succeeded, errors=0**.
- APK ZIP 무결성 및 apksigner 서명 검증 통과.
- 패키지 `com.ecclesia.prayer`, 앱 이름 `에끌레시아`, minSdk 34, targetSdk 36, ARM64 확인.
- APK에 GoogleSignInHelper와 FirebaseAuth 네이티브 클래스 포함 확인.
- APK 서명 지문이 `DEBUG_CERTIFICATE.txt`의 개발 인증서 지문과 일치.

## 미검증 / 사용자 설정 필요

- 연결된 Android 기기가 없어 설치·터치·키보드·오디오 청취는 실기기에서 아직 확인하지 못했습니다.
- 사용자 Firebase 프로젝트의 `google-services.json`이 없어 실제 Google 계정 선택, Firebase 인증, 세션 복원·로그아웃은 아직 확인하지 못했습니다.
- 서버 다중 접속은 이번 구현 범위가 아닙니다.

## 검증 중 관찰 사항

- 초기 APK 빌드는 Google 플러그인의 Kotlin DSL이 비활성화되어 실패했습니다. 프로젝트 빌드 후처리에서 AGP 9 built-in Kotlin을 활성화한 후 재빌드하여 성공했습니다.
- 배치 Play 실행 중 Unity Editor SearchDatabase의 인덱싱 예외가 기록되었습니다. 예외 스택은 UnityEditor.Search 내부였으며 앱 흐름 검증과 화면 캡처는 완료되었습니다. 앱 코드 런타임 예외는 해당 검증에서 발견되지 않았습니다.
- Unity MCP 연결 도구가 노출되지 않았고 에디터 UI 캡처도 시간 초과되어 에끌레시아 내부 검증 사본을 배치 실행했습니다.
- `pixel_war_ship`은 열거나 수정하거나 빌드하지 않았습니다.

## 결과 파일

- `Builds/Android/Ecclesia-prototype.apk`
- `Builds/Android/result.txt`
- `Builds/Validation/navigation.txt`
- `Builds/Validation/runtime.txt`
- `Builds/Validation/01-login.png`
- `Builds/Validation/02-courtyard.png`
- `Builds/Validation/03-prayer.png`

로그와 검증 사본은 `Tools` 아래에 있으며 Git에서 제외했습니다. 실제 배포본은 Firebase 설정 완료 후 다시 빌드해야 합니다.
