# Project North - Development Log
**최종 업데이트**: 2026-02-20 (목) 16:12 KST  
**작업자**: Antigravity AI Assistant  
**Unity Version**: 6000.2.8f1  
**프로젝트 경로**: `C:\Users\user\project_north\Personal_Project_valheim`

---

## 📌 현재 상태 요약

### 핵심 미해결 이슈: Login UI Connect 버튼 클릭 불가
- InputField(ID/PW)는 정상적으로 마우스 입력을 인식하나, **ConnectButton만 클릭이 인식되지 않음**.
- 다수의 자동화 툴을 제작하여 조치했으나, 근본 원인이 완전히 해소되지 않은 상태.
- **다음 세션에서 반드시 Play 모드에서 테스트 후 결과를 확인해야 함.**

### 추정 원인 (조사 완료)
1. ~~StandaloneInputModule 사용~~ → `InputSystemUIInputModule`로 교체 완료
2. ~~배경 Image의 RaycastTarget이 클릭 가로챔~~ → 전체 Graphic 비활성화 후 Selectable만 복구 완료
3. ~~부모 CanvasGroup이 클릭 차단~~ → `ignoreParentGroups = true` 설정 완료
4. **버튼 RectTransform의 sizeDelta가 0x0으로 압축되어 클릭 영역이 물리적으로 존재하지 않을 가능성** → `LayoutElement`와 `sizeDelta` 강제 설정 완료 (확인 필요)

---

## 🔧 오늘 수정/생성한 파일 목록 (2026-02-20)

### 런타임 스크립트

| 파일 경로 | 상태 | 설명 |
| :--- | :---: | :--- |
| `Assets/3.Script/UI/LoginUIController.cs` | 수정 | Connect 버튼 코드 바인딩, "CONNECTING..." 텍스트 변경, 에러 시 복구 로직 |
| `Assets/3.Script/UI/UIHoverFeedback.cs` | 신규 | 마우스 Hover/Click 시 버튼 스케일 변화(1.15x/0.85x) + 디버그 로그 출력 |
| `Assets/3.Script/Network/MySqlAuthenticator.cs` | 수정 | DB 접속 정보를 SerializedField로 분리, connectionString 프로퍼티화 |

### 에디터 툴 스크립트 (`Assets/Editor/Antigravity_Tools/`)

| 파일명 | 메뉴 경로 | 설명 |
| :--- | :--- | :--- |
| `UIInteractionFixer.cs` | Tools/Project North/Fix UI Interaction | StandaloneInputModule → InputSystemUIInputModule 교체 및 비대화형 UI Raycast 비활성화 |
| `UIClickFixer.cs` | Tools/Project North/Fix UI Clicks | EventSystem 완전 초기화 및 Selectable 기반 Raycast 선택 복구 |
| `UltimateUIFixer.cs` | Tools/Project North/Ultimate UI Fix | 전체 Graphic Raycast 일괄 퍼지 후 Selectable만 복구 + UIHoverFeedback 자동 부착 |
| `UIDiagnostics.cs` | Tools/Project North/Run UI Diagnostics | GraphicRaycaster, EventSystem, CanvasGroup, ConnectButton 상태 정밀 진단 및 강제 수리 |
| `ButtonResurrector.cs` | Tools/Project North/Resurrect Connect Button | ConnectButton에 CanvasGroup(ignoreParentGroups=true) 부여, 자식 Text Raycast 해제 |
| `HitboxVisualizer.cs` | Tools/Project North/Fix & Visualize Hitbox | 버튼 Image를 반투명 초록으로 시각화, LayoutElement 최소 크기 확보 |
| `PhysicalButtonFixer.cs` | Tools/Project North/Force Physical Rebuild | RectTransform sizeDelta 강제(200x60), Image를 불투명 빨간색으로 변경, LayoutElement flexible=0 |
| `LoginUIFixer.cs` | - | LoginScene에 ID/PW InputField 자동 추가 및 바인딩 |
| `PlayerSpawnSetup.cs` | Tools/Project North/Setup Player Spawn | Player_New 프리팹에 NetworkIdentity 추가 후 NetworkManager에 연결 |
| `MirrorSceneIDFixer.cs` | Tools/Project North/Fix Mirror Scene IDs | Main/LoginScene 강제 재저장으로 Mirror Scene ID 생성 |

### 기타 수정

| 파일 경로 | 설명 |
| :--- | :--- |
| `Assets/Mirror/Editor/NetworkInformationPreview.cs` | GUI Style 초기화 지연으로 NullReferenceException 수정 |

---

## 🗺️ 다음 세션 작업 가이드

### 1단계: Connect 버튼 클릭 테스트 (최우선)
```
1. Unity 에디터에서 LoginScene을 연다.
2. 메뉴에서 [Tools > Project North > Force Physical Rebuild]를 실행한다.
3. Play 모드에 진입한다.
4. Console 창을 열어 다음 로그가 뜨는지 확인한다:
   - 마우스를 버튼 위에 올렸을 때: "<color=cyan>[UI] Hover ENTER - Mouse Detected!</color>"
   - 버튼 클릭 시: "<color=orange>[UI] Click DOWN</color>"
   - 버튼 클릭 시: "<color=yellow>[LoginUI] Connect Button Clicked!</color>"
5. 화면에 빨간 사각형(버튼 Image)이 보이는지 확인한다.
   - 보이면: 히트박스 존재 확인 → 클릭 테스트 진행
   - 안 보이면: RectTransform이 여전히 0x0 → 부모 LayoutGroup 제거 필요
```

### 2단계: 버튼이 여전히 안 눌릴 경우
```
- Hierarchy에서 ConnectButton을 선택하고 Inspector에서 다음을 수동 확인:
  - RectTransform의 Width/Height가 0이 아닌지
  - Image 컴포넌트가 존재하고 raycastTarget = true인지
  - CanvasGroup의 ignoreParentGroups = true인지
  - 부모 오브젝트에 ContentSizeFitter가 있다면 제거하거나 Unconstrained로 변경
```

### 3단계: 로그인 플로우 전체 테스트
```
1. MySQL 서버가 로컬에서 실행 중인지 확인 (127.0.0.1)
2. NetworkManager_System의 MySqlAuthenticator 컴포넌트에 DB 비밀번호 입력
3. ID/PW 입력 후 Connect → 서버 인증 → Main 씬 전환 확인
```

---

## ⚙️ 기술 설정 참고

### 네트워크 (Mirror)
- **Transport**: KCP (kcp2k)
- **Authenticator**: `MySqlAuthenticator` (커스텀)
- **Online Scene**: Main.unity
- **Offline Scene**: LoginScene.unity
- **씬 전환**: Mirror NetworkManager가 자동 관리 (클라이언트 코드에서 수동 로드 제거됨)

### 데이터베이스 (MySQL)
- **Server**: 127.0.0.1 (로컬)
- **Database**: programming
- **Table**: user_info (User_Name, User_Password)
- **보안**: 파라미터화된 쿼리 사용, 서버 사이드 인증 전용

### 입력 시스템
- **New Input System** 사용 중 (InputSystemUIInputModule)
- Legacy Input은 제거 대상

### 주요 씬
- `Assets/1.Scene/LoginScene.unity` — 로그인 화면
- `Assets/1.Scene/Main.unity` — 메인 게임 씬

---

## 📝 세션별 작업 이력

### 2026-02-20 (세션 2) — UI 클릭 복구 집중 작업
- LoginUIController에 Connect 버튼 피드백 로직 추가
- UIHoverFeedback 컴포넌트 제작 (스케일 반응 + 디버그 로그)
- 6종의 에디터 자동화 툴 제작 (EventSystem 초기화, Raycast 정리, CanvasGroup 강제, 히트박스 시각화, 물리 크기 강제)
- Mirror 씬 전환을 NetworkManager 자동 관리로 변경
- NetworkInformationPreview.cs NullReferenceException 수정

### 2026-02-20 (세션 1) — MySQL 인증 시스템 구축
- MySqlAuthenticator.cs 제작 (서버 사이드 인증)
- LoginUIController.cs 수정 (인증 요청/응답 처리)
- PlayerSpawnSetup, MirrorSceneIDFixer 에디터 툴 제작

### 2026-02-19 — Login UI 리스트럭처
- LoginScene UI를 Valheim 테마로 전면 재구성
- Canvas_Login 계층 구조 설계 및 구현
- LoginUIController.cs, NetworkClientManager.cs 신규 작성
