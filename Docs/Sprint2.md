# Sprint 2 — 탐험 진행과 첫 번째 플레이 루프

[Sprint 1: 전투·조작](Sprint1.md) · [Sprint 3: 아티팩트 빌드](Sprint3.md)

기존 `Sprint02-03.md`에서 탐험 세션·웨이브·경로·귀환·UI 흐름을 분리했다. 아티팩트 정의·효과·보상 후보 생성은 Sprint 3 문서에서 다룬다. Unity 2022.3.62f3 로컬 싱글 플레이 기준이며 네트워크 기능은 구현하지 않았다.

## 1. 기존 프로젝트와 재사용 범위

- Build Settings의 진입 흐름은 `MainMenu → SampleScene`이며 기존 시작 버튼을 재사용한다.
- 독립 Player 프리팹은 없다. 실제 전투 Player와 초상화용 Portrait_Player가 SampleScene에 별도로 배치되어 있다. 탐험은 Player 태그와 PlayerStats가 있는 실제 플레이어만 참조한다.
- 기존 적은 `Assets/Prefabs/Monster/Soju.prefab`이다. MonsterMovement의 플레이어 추적, MonsterCollisionDamage의 접촉 공격, MonsterHurtbox/MonsterStats의 피해 처리를 재사용한다. 탐험 구현 전에는 보스 전용 AI·웨이브·진행 상태·아티팩트 시스템이 없었다.
- 기존 몬스터 사망은 Destroy만 호출했다. 실제 사망에만 한 번 발행되는 OnDied를 추가했으며, SetActive(false)나 풀 반환은 처치로 세지 않는다.
- UGUI, Joystick Pack, 무기 전환 버튼, 양손/단일 스틱, 플릭 회피와 펀치 애니메이션을 유지한다. 전투 코어의 설명과 후속 수정은 [Sprint 1](Sprint1.md)에 있다.
- PoolManager와 SoundManager만 기존 DontDestroyOnLoad 객체다. 탐험 관리자·UI·빌드는 씬/세션 범위이며 새로운 영속 싱글턴을 만들지 않는다.

## 2. 현재 플레이 가능한 흐름

**탐험 준비 → 전투 → 클리어 → 보상 → 경로 선택 → 다음 전투 → 보스 → 최종 보상 → 성공·귀환**

1. MainMenu의 기존 시작 버튼 또는 SampleScene Play로 진입한다.
2. 준비 화면에서 **탐험 시작**을 누른다. 일반 전투 네 번, 특수 전투 한 번, 테스트 보스 한 번을 진행한다.
3. 필수 적 전원 처치 시 고철을 지급하고 보상 단계로 전환한다. 현재 연결된 아티팩트 후보·선택 처리는 [Sprint 3](Sprint3.md)의 시스템을 사용한다.
4. 2–3개 경로 중 하나를 선택한다. 일반/정예는 다음 전투로 바로 이동한다. 정예는 다음 적의 HP와 공격력을 ×1.4로 강화한다.
5. 회복소·상점·랜덤 이벤트는 전투 사이 방문이다. 전투 라운드 인덱스를 소모하지 않는다. 방문 완료 후 **다음 전투로** 이동한다.
6. 최종 보스 처치 후 마지막 보상을 선택하면 성공한다. 등록된 모든 플레이어가 사망하면 실패한다.
7. 결과 화면에서 획득 보상 요약·새 탐험 시작·MainMenu 귀환을 제공한다. 재시작하면 라운드·고철·보상·경로 기록과 탐험 빌드를 초기화한다.

보스는 기존 Soju의 HP·공격력·이동속도를 데이터로 조정한 테스트 전투다. 신규 보스 패턴이나 특수 AI를 구현하지 않았다. 특수 라운드는 기존 적을 조합한 무리 전투다. 고철과 보상은 탐험 안에서만 유지된다.

## 3. 세션 상태와 데이터

| 상태 | 역할 |
| --- | --- |
| Preparing | 탐험 시작 전 준비 화면과 전투 입력 차단 |
| Combat | 일반·정예·특수 전투 |
| Reward | 클리어 보상 및 아티팩트 선택 대기 |
| RouteSelection | 다음 경로 선택 대기 |
| Transition | 회복소·상점·이벤트 방문 및 다음 전투 대기 |
| BossCombat | 마지막 보스 전투 |
| Completed | 탐험 성공·결과 화면 |
| Failed | 전원 사망·실패 결과 화면 |

ExpeditionManager는 명시적인 시작·보상 선택·경로 선택·방문 계속·재시작·귀환 명령으로 상태를 변경한다. 상태·처리 완료 가드가 중복 클리어·지급·재진입을 막는다. UI는 명령과 읽기 전용 상태를 연결하며 라운드 결정·지급 권한을 소유하지 않는다.

ScriptableObject 라운드 데이터는 식별자, 유형, 적 구성·스폰 규칙, 클리어 조건, 보상 테이블 ID, 다음 경로 후보를 표현한다. 현재는 고정 샘플 6개지만 데이터 목록으로 수와 구성을 바꿀 수 있다. 마지막 라운드는 보스로 검증한다.

ExpeditionSession은 시드, 고철, 클리어 수, 획득 보상, 선택 경로를 기록한다. 세션 데이터와 UI 표시 상태는 분리한다.

## 4. 웨이브와 전투 종료

- EnemyWaveSpawner가 라운드 데이터로 기존 몬스터 프리팹을 생성한다.
- 필수 적의 생존 집합을 추적하고 실제 사망 이벤트를 한 번만 처리한다. 선택 적이 있더라도 필수 적 전원 처치 조건을 구분한다.
- 오브젝트 비활성화·풀 반환은 사망 이벤트로 간주하지 않는다. 중복 스폰·사망 통지로 라운드를 여러 번 종료하지 않는다.
- 전원 사망 시 실패 처리하며 잔여 적과 활성 탄환을 정리한다. 라운드 전환·정상 종료에서도 잔여 전투 오브젝트를 정리한다.
- 플레이어 입력 허용 여부는 상태에 맞춰 관리한다. 선택 화면에서 남아 있는 조이스틱·버튼 입력을 차단하는 후속 UI 처리는 [MobileUI.md](MobileUI.md)에 있다.

## 5. 경로 규칙

| 경로 | 현재 처리 | 전투 라운드 소비 |
| --- | --- | --- |
| 일반 전투 | 다음 전투로 이동 | 다음 전투 시작 시 진행 |
| 정예 전투 | 다음 적 HP·공격력 ×1.4 | 다음 전투 시작 시 진행 |
| 회복소 | 생존 플레이어 최대 HP 30% 회복 | 방문 자체는 소비하지 않음 |
| 상점 | 고철 10개로 최대 HP 40% 회복, 방문당 한 번 구매 | 방문 자체는 소비하지 않음 |
| 랜덤 이벤트 | 시드 기반으로 고철 15개 또는 HP 20% 회복 | 방문 자체는 소비하지 않음 |

상점에서 HP가 가득 차면 결제하지 않는다. 회복·상점·이벤트 방문은 완료 후 다음 전투로 이동한다. 경로 후보는 데이터로 관리하며 최종 전투는 보스로 진입한다. 상점과 이벤트는 최소 흐름이며 본격적인 상품 목록·인벤토리·이벤트 콘텐츠는 구현하지 않았다.

## 6. 주요 파일과 씬 연결

경로는 `Bottle Game/Assets` 기준이다.

| 경계 | 파일과 역할 |
| --- | --- |
| 진행 데이터 | `Scripts/Expedition/ExpeditionDefinition.cs`, `ExpeditionRoundDefinition.cs`, `ExpeditionRouteDefinition.cs`: 라운드·스폰·클리어·보상 테이블·경로 |
| 세션 기록 | `Scripts/Expedition/ExpeditionSession.cs`: 시드·고철·클리어·보상·경로의 읽기 전용 기록 |
| 진행 명령 | `Scripts/Expedition/ExpeditionManager.cs`: 상태, 명시적 명령, 입력 허용, 종료·재시작·귀환 |
| 웨이브 | `Scripts/Expedition/EnemyWaveSpawner.cs`: 생성, 필수 생존 집합, 중복 방지, 잔여 적 정리 |
| 표시 연결 | `Scripts/Expedition/ExpeditionUI.cs`: 세션 명령·데이터를 UI에 바인딩 |
| 표시 참조 | `Scripts/UI/ExpeditionUIView.cs`, `ExpeditionChoiceView.cs`: 화면·버튼·카드 참조와 표시 |
| 기존 기능의 최소 확장 | `Scripts/Monster/MonsterStats.cs`: 사망/스폰 초기화. `Scripts/Player/PlayerStats.cs`: 회복·재시작과 HP 처리. 기존 PoolManager/SoundManager 중복 생성 방지 |
| 에셋 연결 | `Editor/ExpeditionSetup.cs`, `Data/Expedition/**`, `Scenes/SampleScene.unity` |
| 검증 | `Tests/Editor/Sprint02ExpeditionTests.cs`, `Sprint02UIIntegrationTests.cs` |

SampleScene의 `Expedition Session`에 관리자·스포너·UI 컨트롤러를 연결한다. 기존 연습용 Soju 세 개는 삭제하지 않고 비활성화하여 보존한다. 아티팩트 데이터·전투 보정 연결 파일은 [Sprint 3](Sprint3.md)에 따로 정리한다.

라운드·경로·보상 데이터는 Inspector에서 편집한다. **Bottle Game > Expedition > Rebuild Sprint 02-03 Sample**은 기존 이름을 유지한 샘플 재생성 메뉴다. 사용자 설정을 유지하려면 일상적으로 실행하지 않는다. 문서 번호 정리는 이 메뉴나 테스트 클래스의 이름을 변경하지 않는다.

## 7. 후속 UI 정리와 직접 커스터마이징

- 조작 UI와 탐험 UI를 UIRoot 아래 별도 Canvas로 관리한다. 안전 영역, 모바일 버튼 크기, 스크롤, HUD 입력 통과, 모달 입력 차단을 적용했다.
- 화면·버튼을 매번 생성·삭제하지 않고 직렬화된 프리팹과 카드를 재사용한다. HUD는 전투 중 0.2초 간격으로 갱신한다.
- UITheme/UIThemeBinding/UIThemeScope로 스프라이트·폰트·버튼 상태를 지정한다. Text/TMP, 카드 프리팹, 전체 UI 프리팹 교체를 지원한다.
- 배치·모바일 입력 편집: [모바일 UI 정리](MobileUI.md).
- 임의 UI 세트·카드·전체 프리팹 연결: [UI 커스터마이징](UICustomization.md).

UI 기능 검사는 성능 측정 결과가 아니다. 새 UI 세트의 한글 폰트·실기기 터치·투명 이미지 오버드로와 CPU/GPU 프레임 시간은 별도로 확인해야 한다.

## 8. 확장 계약과 범위 제한

- `RoundCompleted`, `RewardSelected`, `ExpeditionEnded`, `Changed` 이벤트를 외부 시스템이 구독할 수 있다.
- `Session.Rewards`, `Session.SelectedRoutes`, `LastSelectedRoute`로 획득 기록·선택 경로를 읽을 수 있다.
- NGO/Relay/Lobby·영구 성장·인벤토리·제작·농장·새 액체 시스템은 추가하지 않았다. 기존 적 AI를 대규모로 재구현하지 않았다.
- 향후 서버가 라운드·후보·처치·지급을 결정하고 UI는 승인된 결과를 표시하도록 확장한다. 플레이어 ID별 명령/선택 승인, 난수 스트림과 시퀀스 관리가 필요하다.
- 현재 적 AI의 Player 태그 탐색은 협동 타깃 선택의 변경 지점이다. 실제 보스 패턴, 완성형 상점·이벤트·허브와 영구 보상은 후속 범위다.

## 9. 검증 기록과 남은 확인

Unity 2022.3.62f3의 분리된 프로젝트 복사본에서 배치 실행했다. CLI 진입은 EditMode지만 EnterPlayMode/ExitPlayMode로 실제 플레이 루프를 실행한다. 여러 묶음을 한 번에 실행할 때 일부가 건너뛰는 현상이 있어 묶음마다 별도 프로세스에서 완료 마커를 확인했다.

| 검사 | 기록 | 결과 |
| --- | --- | --- |
| 탐험 7개 시나리오 | 비활성화/사망 구분, 필수/선택 적, 중복·재진입, 6라운드·보스·5경로, 빈 후보, 전원 사망, 10회 재시작, 실제 MainMenu 귀환·재진입 | 통과 |
| 초기 전체 UI 흐름 1개 | 실제 첫 웨이브 자동 조준·Gun·탄환 충돌, 보상·6라운드·결과·재시작·실패·귀환, EventSystem 입력 판정 | 통과 |
| 모바일 UI 정리 후 | 실제 Raycast 클릭·스크롤·전투/모달 입력 차단·재활성화·전체 탐험 | 통과 |
| UI 커스터마이징 후 | 기존 UI 전체 흐름 회귀 | 통과 |
| 별도 UI 교체 검사 | 테마·Text/TMP·카드/전체 프리팹 교체와 세션 명령 유지 | 통과 |

기존 종합 문서의 초기 총 40개는 전투 18 + 탐험 7 + 아티팩트 14 + UI 전체 흐름 1의 합이었다. 전투 검증은 [Sprint 1](Sprint1.md), 아티팩트 검증은 [Sprint 3](Sprint3.md)에 기록하며 서로 다른 실행 시점의 수를 합산하지 않는다.

재실행 시 Unity CLI의 `-runTests -testPlatform EditMode -testFilter Sprint02ExpeditionTests`를 사용한다. UI 검사는 필터를 `Sprint02UIIntegrationTests`, 교체 검사는 `UICustomizationTests`로 바꾸어 각각 별도 실행한다. UI 완주 검사의 첫 웨이브는 실제 탄환으로 처치하며 이후 전투는 사망 데미지를 주입해 진행과 버튼 연결을 검증한다.

실제 모바일 기기의 터치·한글 폰트·안전 영역·프레임 성능, 난이도 밸런스와 수동 시각 검수는 별도 확인 대상이다. 문서 재정리를 이유로 새로운 Unity 테스트를 실행하지 않았으며 위 결과는 기존 구현 시점의 기록이다. 원본 실행 결과 XML은 삭제했고 검증 요약과 재실행 방법을 유지한다.
