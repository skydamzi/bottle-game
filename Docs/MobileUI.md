# 모바일 UI 정리

직접 만든 UI 세트·테마·카드·전체 레이아웃 교체는 [UI 커스터마이징 안내](UICustomization.md)를 참고한다. 현재 텍스트 슬롯은 UGUI Text와 TextMeshProUGUI를 모두 지원한다.

## 편집 위치

- 탐험 UI: `Bottle Game/Assets/Prefabs/UI/ExpeditionUI.prefab`을 Prefab Mode에서 연다. 루트의 `ExpeditionUIView`에서 각 화면, 버튼, 텍스트 참조를 확인한다.
- 조이스틱·무기 전환: `SampleScene > UIRoot > ControlsCanvas > SafeArea > Controls`.
- 초상화: 같은 SafeArea 아래 `HUD`. 메뉴 버튼: `UtilityButtons`.
- 메인 메뉴: `MainMenu > UIRoot > MainMenuCanvas > SafeArea`.
- 공통 폰트·드래그 민감도: 각 씬의 `UIRoot > MobileUIRoot`.

```text
UIRoot
├─ ControlsCanvas
│  └─ SafeArea
│     ├─ HUD
│     ├─ Controls
│     └─ UtilityButtons
└─ ExpeditionUI (prefab)
   ├─ HUDCanvas / SafeArea / Combat HUD
   └─ ModalCanvas / Decision Overlay / SafeArea
      └─ Decision Scroll / Viewport / Decision Content
         ├─ PreparingScreen
         ├─ RewardScreen / RewardCard_01..03
         ├─ RouteScreen / RouteCard_01..03
         ├─ VisitScreen
         ├─ ResultScreen
         └─ BuildDetails
```

화면 크기·위치·폰트는 프리팹의 RectTransform, LayoutElement, Text에서 수정한다. 카드의 최소 높이는 240, 일반 버튼은 144이며 1080×1920 기준이다. 너비 360 기준 일반 버튼 높이는 약 48에 해당한다. 조이스틱은 320, 무기 전환은 176, 메뉴는 160 크기로 배치했다. 기존 조이스틱 컴포넌트와 버튼 UnityEvent 연결은 유지한다.

## 역할 분리

| 파일 | 역할 |
| --- | --- |
| 기존 `Scripts/Expedition/ExpeditionUI.cs` | 세션 명령·이벤트와 화면 데이터를 연결. UI 생성·삭제 제거. HUD는 전투 중 0.2초 간격으로 갱신하고 같은 문자열의 재할당을 피함. |
| `Scripts/UI/ExpeditionUIView.cs` | 직렬화된 화면 참조, 버튼 이벤트 구독·해제, 표시 전환. 세션 진행 규칙은 관리하지 않음. |
| `Scripts/UI/ExpeditionChoiceView.cs` | 재사용하는 보상·경로 카드의 텍스트·아이콘·클릭 연결. |
| `Scripts/UI/MobileSafeArea.cs` | 화면 크기 또는 Safe Area 변경 때만 앵커 갱신. |
| `Scripts/UI/MobileGameplayControls.cs` | 전투 상태에 맞춰 기존 조작 CanvasGroup 입력 차단. 모달이 열리면 눌린 입력을 초기화. |
| `Scripts/UI/MobileUIRoot.cs` | 씬당 공통 폰트, 화면 배율에 맞는 드래그 임계값. |
| `Editor/MobileUISetup.cs` | 편집 가능한 샘플 프리팹 생성 및 기존 씬 연결 도구. |

HUD와 모달을 별도 Canvas로 나눴다. 표시 전환에는 CanvasGroup을 사용한다. 전투 HUD의 장식·텍스트는 Raycast 대상에서 제외하고, 모달은 Safe Area 바깥까지 입력을 차단한다. 선택 화면의 스크롤은 기본 ScrollRect를 사용한다. HUD 갱신 때 전체 레이아웃을 강제로 재계산하지 않는다.

`ExpeditionSetup.Install`은 탐험 씬에 기존 UI 프리팹을 연결한다. 이미 편집한 프리팹을 재생성하지 않는다. 별도 메뉴 **Bottle Game > UI > Rebuild Mobile UI Sample**은 샘플 프리팹과 두 씬의 UI 배치를 다시 생성하므로 사용자 지정 디자인 작업 전에만 사용한다. 정상 플레이를 위해 실행할 필요는 없다. 기존 전투 코드, 패키지, 화면 방향 설정은 이번 작업에서 변경하지 않았다.

## 확인할 환경

Unity 2022.3.62f3의 분리된 테스트 프로젝트에서 컴파일과 Play Mode 검증을 수행했다. 테스트 진입 명령은 EditMode이지만, 각 테스트가 EnterPlayMode를 통해 실제 플레이 루프를 실행한다.

- UI 통합 테스트 통과: 실제 EventSystem 최상위 Raycast로 시작·보상·경로·결과 버튼 클릭, 넘치는 카드의 ScrollRect 드래그, 준비/보상 화면의 전투 입력 차단, 전투 중 조이스틱 Raycast 통과, 모달 전환 뒤 늦게 놓은 무기 버튼 차단.
- 전체 6라운드, 물리 탄환으로 첫 전투 클리어, 성공·재시작·실패·메인 메뉴 귀환·재진입 통과.
- UI 갱신 및 한 탐험 동안 오브젝트 수 유지, 컨트롤러 세 차례 재활성화 후 중복 버튼 구독 없음 확인.
- 기존 전투 회귀 21개 시나리오 통과: 자동 타겟/발사, 레거시 조작, 펀치 복귀·방향 고정, 이동·플릭 회피, 풀 및 씬 참조 검사 포함.
- 기존 두 씬의 직렬화된 오브젝트 ID 전부 보존 확인. 런타임 및 에디터 C# 컴파일 통과. 원본 실행 결과 XML은 정리하면서 삭제했다.

프리팹에는 기존 내장 폰트를 두고 실행 시 공통 폰트를 적용한다. `MobileUIRoot.font`를 비워 두면 OS 폰트를 사용한다. 배포 시에는 한글 글리프와 모바일 임베딩 라이선스를 갖춘 폰트 에셋을 이 필드에 지정해야 기기별 표시를 보장할 수 있다. 이번 작업에서는 새 폰트·패키지를 설치하지 않았다.

실기기에서는 9:16 / 9:19.5 화면, 노치·하단 홈 영역, 두 손가락 조이스틱+무기 버튼, 카드 드래그 중 선택 취소를 확인한다. 실제 Android/iOS 터치, 그래픽 출력 품질과 Profiler 성능 측정은 자동 테스트와 별도로 확인해야 한다.
