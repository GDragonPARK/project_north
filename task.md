# 🎮 Valheim Clone Project Task List

## 🏁 현재 상태 (Current Status)
- [x] 기본 테스트 환경 구축 (임시 바닥 생성 스크립트 적용)
- [x] 프로젝트 기능 정의서 분석 완료
- [x] 지형 생성 및 식생 자동 배치 시스템 완료
- [x] 벌목 물리 및 플레이어 액션 기초 시스템 완료

---

## 🛠️ 주요 개발 태스크 (Major Tasks)

### 1. 플레이어 컨트롤러 고도화 (Player Controller)
- [x] New Input System 패키지 확인 및 기본 세팅
- [x] `MyPlayerController.cs`를 Rigidbody + New Input System 기반으로 변경
- [x] 스테미나(Stamina) 시스템 구현 (달리기, 점프, 공격 소모)
- [x] 레이캐스트 기반 도끼 공격(Axe Swing) 및 벌목 연동
- [x] 공격 애니메이션 트리거 (`Attack`) 연결
- [ ] Human Melee Animations 에셋 연동 및 Animator Controller 세부 설정

### 2. 지형 및 환경 시스템 (Terrain & Environment)
- [x] 펄린 노이즈 기반 `TerrainGenerator` 구현 (실시간 조절 및 URP 셰이더 적용)
- [x] 고도 및 경사도 기반 식생 자동 스포너 (`VegetationSpawner`) 구현
- [x] `TreeFelling` (나무 쓰러짐) 물리 로직 구현 (HP, 방향성 쓰러짐)
- [ ] Weather Maker 기본 셋업 (안개/비 설정)

### 3. 건축 시스템 기초 (Building System)
- [x] 건축 모드 진입 로직 및 `InventorySystem` 자원 체크 연동
- [x] 청사진 시스템 (Ghost Preview) 및 자원 가용 여부에 따른 색상 변경 구현
- [x] 포인트 기반 스냅 시스템 (SnapPoint) 설계 및 거리 기반 자석 효과 구현
- [x] 해체 시스템 (Deconstruct) 및 자원 환급 로직 구현 (오른쪽 클릭)
- [x] 구조적 안정도 기초 (Stability Check) 구현 (지면 미접촉 시 무너짐)
- [x] 건축 메뉴 (Building Menu) 프로토타입 UI 연동
- [ ] 구조적 안정도 알고리즘 고도화 (재질별 거리 제한 등)

### 4. 데이터 및 UI (Data & UI)
- [x] ScriptableObject 기반 `ItemData` 구조 설계 (공격력, 스테미나 연동 완료)
- [x] 스테미나 게이지 시각화 (`StaminaUI`) 구현 (Slider & 자동 숨기기 적용)
- [x] `CraftingRecipe` 시스템 구축 (재료 소모 및 제작 결과 연동)
- [x] 제작대(Workbench) 기반 거리 제한 및 레벨 시스템 구현
- [x] 제작 UI (Crafting UI) 프로토타입 구현
- [x] 음식 및 버프 시스템 (`FoodSystem`) 구현 (최대 3개, 시간에 따른 감소)
- [x] 음식 UI (Food Buff UI) 프로토타입 구현
- [x] 보관 시스템 (`StorageContainer`) 및 아이템 주고받기 기능 구현
- [x] 보관함 UI (`StorageUI`) 프로토타입 연동
- [x] 적 AI 기초 (`EnemyAI`) 구현 (Idle, Wander, Chase, Attack FSM)
- [x] 전투 및 전리품(Loot) 드랍 시스템 연동
- [ ] 인벤토리 슬롯 아이템 실시간 연동 및 UI 레이아웃 작업 (Fantasy RPG GUI 적용)
- [ ] 사냥 시스템 고도화 및 적 종류 추가 (넥, 드베르그 등)

### 5. 물리 및 환경 (Physics & Environment)
- [x] 지면 연결 기반 구조 안정도(Stability) 시각화 및 연동
- [x] 낮과 밤의 순환 (`TimeManager`) 및 태양 각도/강도 자동 제어 구현
- [x] 시간대별 환경 변화 (URP Fog 및 Skybox 연동) 프로토타입
- [x] 밤 시간대 적(EnemyAI) 공격력 및 감지 범위 강화 시스템 구현
- [x] 날씨 시스템 (`WeatherManager`) 구현 (맑음, 비, 폭풍우 순환)
- [x] 환경 디버프 (`Wet`, `Cold`) 및 스테미나/체력 회복 속도 연동
- [x] 쉘터(Shelter) 및 불(Fire) 감지 기반 디버프 해제 로직 구현
- [ ] 시간 흐름에 따른 기온 변화 및 요리 시스템 연동

### 6. 시스템 및 최적화 (Systems & Optimization)
- [x] JSON 기반 데이터 직렬화 (`SaveManager`) 구현 (플레이어, 시간, 설치물 저장)
- [x] 월드 상태 보존 (보관함 내부 아이템 및 건축물 위치 동기화)
- [x] 자동 저장(Auto-Save) 및 퀵 세이브/로드(F5/F9) 기능 구현
- [x] 보스 소환 시스템 (`BossSummonAltar`) 및 제단 상호작용 구현
- [x] 보스 AI (`BossAI`) 구현 (돌진, 광역 번개 공격 패턴)
- [x] 보스 전용 HP UI (`BossUI`) 연동
- [x] 실시간 미니맵 (`MinimapCamera`) 및 렌더 텍스처 연동
- [x] 안개 시스템 (`Fog of War`) 및 플레이어 위치 기반 맵 밝히기
- [x] 미니맵 마커 (`MinimapMarker`) - 플레이어(방향), 집, 보스 제단 아이콘 표시
- [x] 인벤토리 및 제작 UI 최종 폴리싱 (Fantasy RPG GUI 스타일 적용)
- [x] 툴팁(Tooltip) 시스템 및 제작 프로그레스 바 연동
- [x] 안개 데이터 (`FogOfWar`) 저장 및 불러오기 기능 보완

---

## 📅 로그 (Log)
- **2026-01-27**: 프로젝트 분석 및 `task.md` 생성. 임시 바닥 생성 도구 추가.
- **2026-01-28**:
    - `Terrain/Vegetation`: 노이즈 기반 지형 및 나무/풀 자동 배치 시스템 완성.
    - `Construction/Physics`: 구조 안정도 기반 건축 및 도끼 벌목 물리 구현.
    - `Survival/Food`: 다중 음식 섭취 버프 및 시간에 따른 능력치 감소 시스템.
    - `Combat/AI`: FSM 기반 적/보스 AI 및 소환 제단 시스템 구축.
    - `Environment`: 낮밤 순환(TimeManager), 날씨(WeatherManager), 디버프(Wet/Cold) 연동.
    - `System/UI`: JSON 세이브/로드, 미니맵, 안개 데이터 보존 및 최종 UI 폴리싱 완성.
