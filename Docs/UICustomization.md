# 직접 만든 UI 세트 연결

이 구조는 Unity UGUI(Canvas)의 스프라이트·Button·Text·TextMeshProUGUI를 사용한다. UI Toolkit의 UXML은 별도 UI 시스템이므로 이 슬롯에 직접 연결하지 않는다. 게임 진행 규칙은 ExpeditionManager에 두고, 화면은 ExpeditionUIView 계약으로 교체한다. 이름으로 버튼을 자동 검색하거나 임의의 프리팹에 게임 규칙을 주입하지 않는다.

## 가장 간단한 방법: 디자인만 교체

1. `Assets/Prefabs/UI/ExpeditionUI.prefab`을 Prefab Mode로 연다. 원본을 보존하려면 Prefab Variant로 작업한다.
2. 루트의 **UI Theme Scope > Theme**에서 `Assets/Data/UI/DefaultUITheme.asset`을 확인한다. 이 에셋을 복제하거나 Project의 **Create > Bottle Game > UI > Theme**로 본인 테마를 만든다.
3. 테마의 `Panel`, `Primary Button`, `Secondary Button`, `Reward Card`, `Route Card`, `HUD`에 제작한 Sprite를 넣는다. 텍스처는 **Sprite (2D and UI)**로 임포트한다. 9-slice를 쓰면 Sprite Editor에서 Border를 설정하고 테마의 Image Type을 Sliced로 둔다.
4. 버튼 클릭 이미지는 **Apply Button States**를 켜고 Transition을 Sprite Swap으로 선택한 뒤 Pressed/Highlighted/Disabled Sprite를 지정한다. 색 변화라면 Color Tint와 Colors를 사용한다.
5. 프리팹 루트의 Theme Scope에서 **Apply Theme / Preview**를 누르고 저장한다. 테마 에셋 Inspector의 **Preview Theme on Loaded UI**로 열린 씬/Prefab Mode의 같은 테마 UI를 함께 미리 볼 수도 있다. Undo를 지원한다. 실행 시에도 테마가 적용된다.

Sprite를 비우면 기존 이미지가 유지된다. **Apply Tint**를 켰을 때만 테마 색상이 적용된다. 테마는 위치·앵커·버튼 크기를 바꾸지 않는다. 각 이미지의 UIThemeBinding에서 Role을 선택하면 어떤 스타일을 받을지 정할 수 있다. 바꾸고 싶지 않은 장식에는 이 컴포넌트를 붙이지 않는다. 중첩된 Theme Scope는 자체 테마를 사용한다.

기본 보상 카드는 종전 등급 색상을 유지하기 위해 **Tint Background By Rarity**가 켜져 있다. 제작한 카드 그림을 원래 색으로 쓰려면 이 옵션을 끄고, 등급 표시용 테두리/배지가 있으면 **Rarity Accent**에 연결한다. 컨트롤러는 아트워크 색상을 하드코딩하지 않는다.

## 조이스틱·메뉴·무기 버튼

`SampleScene > UIRoot`의 UIThemeScope도 같은 테마 에셋을 참조한다. Joystick Base/Handle, Weapon Button, Menu Button, Portrait Frame 스타일로 교체한다. `MainMenu > UIRoot`에서도 테마를 지정할 수 있다.

기존 조이스틱의 Background/Handle RectTransform 및 Button의 UnityEvent는 조작과 연결되어 있다. 배경 이미지나 자식 장식을 바꾸되 해당 컴포넌트와 참조를 유지한다. 레이아웃을 직접 바꿀 때는 `ControlsCanvas > SafeArea > Controls`에서 작업한다. 테마 적용은 Raycast Target이나 CanvasGroup의 전투 입력 차단을 변경하지 않는다.

## 카드 레이아웃까지 교체

기본 템플릿은 다음 두 프리팹이다.

- `Assets/Prefabs/UI/RewardCard.prefab`
- `Assets/Prefabs/UI/RouteCard.prefab`

템플릿을 복제/Variant로 만들고 내부 이미지를 원하는 UI 세트로 바꾼다. 루트의 **ExpeditionChoiceView**에 Button, Title, Description, 선택적 Icon/Rarity Accent를 연결한다. Title과 Description은 UGUI Text와 TextMeshProUGUI를 모두 받는다. 동적 이름·설명은 이 두 슬롯만 갱신한다. 장식용 텍스트는 별도로 자유롭게 추가한다.

전체 UI 프리팹 루트의 **ExpeditionUIView > Optional Card Prefab Overrides**에 Reward Card Prefab / Route Card Prefab을 넣는다. 실행 시 기존 세 카드 위치에 템플릿을 각각 한 번 생성한다. 보상 갱신이나 UI 재활성화 때 새로 만들지 않는다. 템플릿을 비우면 배치된 기존 카드를 사용한다. 슬롯 인덱스는 배열 순서대로 자동 연결하므로 복제된 카드의 Choice Index를 일일이 설정할 필요가 없다.

슬롯 위치·앵커·기본 크기는 기존 배치를 사용한다. 내부 레이아웃과 LayoutElement의 최소/선호 높이는 카드 프리팹에서 편집한다. 카드 프리팹 교체는 Play 진입 시 적용된다. Prefab Mode에서는 템플릿 자체를 열어 디자인한다.

## 전체 화면 구조 교체

1. `ExpeditionUI.prefab`의 Variant 또는 복제본을 만든다. 안전 영역, CanvasGroup, 버튼 참조를 유지한 채 화면별 계층과 디자인을 수정한다.
2. 새 프리팹 루트의 ExpeditionUIView에 HUD/Modal CanvasGroup, 준비·보상·경로·방문·결과 화면, 액션 버튼, 서로 다른 보상 카드 3개와 경로 카드 3개를 연결한다. Inspector의 경고로 누락·중복 참조를 확인한다. GameObject 이름은 자유롭게 바꿀 수 있다.
3. 텍스트 슬롯에는 `Text` 또는 `TextMeshProUGUI` 컴포넌트를 드래그한다. Image를 잘못 연결하면 Inspector가 경고한다. Purchase Label도 직접 지정할 수 있다. ScrollRect는 선택 사항이며 고정된 카드 배치도 가능하다.
4. `SampleScene > Expedition Session > ExpeditionUI`의 **View Prefab**에 만든 프리팹을 넣는다. **View Parent**는 연결된 UIRoot를 사용한다.
5. 실행하면 기본 씬 UI는 비활성화되고 선택한 프리팹이 한 번 생성된다. 기본 씬 UI 참조는 보존되므로 View Prefab을 비우고 다시 실행하면 기본 UI를 사용한다.

처음부터 만든 UI 세트도 동일한 ExpeditionUIView 참조 계약을 연결하면 사용할 수 있다. 기본 사각형 레이아웃을 유지할 의무는 없다. 모달에는 화면 전체를 덮는 Raycast 차단 이미지를 두고, HUD 장식은 Raycast Target을 꺼야 기존 모바일 조작이 유지된다. 클릭 이벤트는 스크립트가 연결하므로 시작/보상/경로 버튼에 같은 게임 명령을 UnityEvent로 중복 등록하지 않는다. 별도의 사운드·연출 이벤트는 유지한다.

## 폰트와 리소스

이미 설치된 TextMeshPro 3.0.7의 **Essential Resources**를 가져와 기본 설정·폰트·셰이더를 포함했다. 새 패키지는 추가하지 않았다. 기본 Liberation Sans SDF는 한글 UI용 폰트가 아니다. 실제 사용할 한글 TMP Font Asset을 테마의 TMP Font 또는 각 TMP 텍스트에 지정한다. 기존 UGUI Text의 OS 폰트 대체 처리는 유지하며, 배포용 Font 에셋은 UITheme.Font 또는 MobileUIRoot.Font에 지정한다. 테마 폰트를 비우면 제작한 각 텍스트의 폰트를 유지한다.

## 도구와 변경 경계

- `Bottle Game > UI > Connect Customization Slots`: 기본 프리팹과 두 씬에 테마 연결점을 추가한다. 기존 테마·템플릿이 있으면 다시 만들지 않는다. 카메라와 전투 설정을 수정하지 않는다.
- `Rebuild Mobile UI Sample` 및 `Rebuild Sprint 02-03 Sample`은 샘플 재생성 도구다. 직접 편집한 UI에 일상적으로 실행할 필요가 없다. 특히 UI Rebuild는 샘플 디자인을 다시 만든다.
- `UITheme`는 공유 아트·폰트·버튼 스타일 데이터, `UIThemeBinding`은 개별 그래픽의 역할, `UIThemeScope`는 적용 범위다. 이 컴포넌트들은 프레임마다 UI를 검색하거나 테마를 적용하지 않는다.
- `ExpeditionUIView`는 참조와 표시, `ExpeditionChoiceView`는 카드 콘텐츠와 클릭, 기존 `ExpeditionUI`는 세션 데이터와 명령을 연결한다. 이동·공격·회피 및 탐험 진행 규칙은 UI 디자인과 분리한다.

## 성능 판단의 범위

현재 코드 검토에서 UI가 매 프레임 오브젝트를 생성·삭제하거나 전체 레이아웃을 강제 재계산하는 경로는 없다. HUD 갱신은 0.2초 간격이며 같은 값의 Text/TMP 텍스트를 다시 설정하지 않는다. 카드/화면 교체 인스턴스는 초기화 때만 생성한다. 비활성 기본 UI/카드는 복귀용으로 유지되므로 교체 모드에는 소량의 추가 메모리가 든다. 같은 상태에서 켜고 끌 때 인스턴스가 늘어나면 안 된다.

이것은 **실기기에서 병목이 없다는 측정 결과가 아니다.** 새 UI 세트의 큰 투명 이미지·겹침, 마스크/머티리얼/폰트별 배치 분리, 동적 TMP 글리프 생성은 비용을 늘릴 수 있다. Sprite Atlas와 공유 머티리얼을 고려하되, 실제 기기에서 확인한 뒤 적용한다.

현재 함께 확인할 지점은 피격마다 출력되는 MonsterStats 로그, 타겟 탐색 버퍼(64개) 초과 시의 배열 할당, RunBuild 수치 조회의 반복 비용, 카메라 Pixel Perfect의 실제 렌더 해상도 및 GPU 시간이다. 이 지점들을 이번 UI 작업에서 임의로 전투 리팩터링하지 않았다. 현재 저장된 Reference Resolution은 1440×2560이지만 Crop Frame은 꺼져 있어 실제 렌더 크기는 기기 해상도와 정수 배율에 따라 달라진다.

Profiler에서 `BottleGame.UI.Refresh`와 `BottleGame.UI.HUD` 마커로 UI 바인딩 시간을 분리해 볼 수 있다. 실제 모바일 Development Build에서 CPU/GPU 프레임 시간, GC Alloc, Canvas.BuildBatch, 오버드로를 확인해야 한다. 자동 기능 검사는 프레임 예산·실기기 터치·시각 품질을 보장하지 않는다.

## 변경 파일과 검증

| 변경 범위 | 파일 |
| --- | --- |
| 테마 데이터·그래픽 역할·적용 범위 추가 | `Scripts/UI/UITheme.cs`, `UIThemeBinding.cs`, `UIThemeScope.cs` |
| Text/TMP, 카드 교체, 참조 계약 개선 | 기존 `Scripts/UI/ExpeditionUIView.cs`, `ExpeditionChoiceView.cs` |
| 늦게 생성된 UI의 기본 폰트 처리 | 기존 `Scripts/UI/MobileUIRoot.cs` |
| 전체 프리팹 교체, 하드코딩 카드 색상 제거, Profiler 마커 | 기존 `Scripts/Expedition/ExpeditionUI.cs` |
| 참조 검사·Undo 미리보기·기존 샘플 연결 | `Editor/UICustomizationInspector.cs`, `UICustomizationSetup.cs`, 기존 `MobileUISetup.cs` |
| 프리팹·테마·씬 연결 | `Prefabs/UI/ExpeditionUI.prefab`, 신규 `RewardCard.prefab`, `RouteCard.prefab`, `Data/UI/DefaultUITheme.asset`, `Scenes/MainMenu.unity`, `SampleScene.unity` |
| 설치된 TMP 기본 리소스 | `Assets/TextMesh Pro`와 해당 메타 파일 |
| 교체 검증 | `Tests/Editor/UICustomizationTests.cs`, 아래 검증 요약 |

Unity 2022.3.62f3 분리된 프로젝트에서 실제 Play Mode를 진입해 다음을 검증했다. CLI 진입은 EditMode이며 각 통합 테스트가 EnterPlayMode로 플레이 루프를 실행한다.

- 커스텀 테마의 Sprite/9-slice/버튼 상태 적용과 빈 스타일의 원본 색상·크기 유지.
- 다른 전체 화면·보상/경로 카드 프리팹 교체, TMP 텍스트 갱신, 원본 아트 색상 보존.
- 직접 복제한 카드 및 교체 카드의 배열 인덱스 연결, 실제 최상위 Raycast로 마지막 보상·회복소 경로 선택, 비활성 카드 재클릭 시 중복 지급 차단.
- 컨트롤러 4회 재활성화 후 클릭 이벤트 중복 없음, 카드 인스턴스 추가 생성 없음.
- 기존 UI 6라운드 탐험·물리 자동 발사·보상·경로·성공·실패·재시작·메인 메뉴 귀환/재진입 통합 테스트 통과.
- 기존 전투 회귀 21개 시나리오 통과. 런타임·에디터·테스트 C# 컴파일 통과.
- 기존 두 씬의 모든 오브젝트 ID와 Camera 컴포넌트 설정 보존 확인. 이동/공격/회피·탐험 진행 규칙 및 패키지 manifest 변경 없음.

사용자가 넣을 실제 아트 세트의 레이아웃·한글 TMP 폰트 표시와 Android/iOS 실기기 터치 및 성능 측정은 남아 있다. 테스트의 TMP 기본 폰트는 한글 표시 품질 검증용이 아니며, 제작한 한글 폰트 에셋을 연결해야 한다.
