# Transmog (SPT 4.1 포팅) — Transmog-zzap--Bootleg-

### ⚠️ IMPORTANT NOTICE / DISCLAIMER

**Original Author:** szszss (Transmog) · IcyClawz (Custom Interactions; 소스 저장소 IgorEisberg)
**Original Repository:** EFT.Transmog / SPT-ClientMods (CustomInteractions)
**Original Link:** https://github.com/szszss/EFT.Transmog · https://github.com/IgorEisberg/SPT-ClientMods · https://hub.sp-tarkov.com/files/file/1278-custom-interactions/
**License notation:** Transmog — MIT / Custom Interactions — University of Illinois/NCSA Open Source License (전문: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md))

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

## 변경 이력

- 2026-10-12 19:16 — 릴리즈 노트 작성([release-notes/v1.3.1.1.md](release-notes/v1.3.1.1.md) GitHub용 한/영, [v1.3.1.1-arca.txt](release-notes/v1.3.1.1-arca.txt) 아카라이브용). 사용자 PC에서 동작 확인된 상태로 기록.
- 2026-10-12 19:08 — 4.1 첫 포팅. Transmog 1.3.1 + Custom Interactions 1.8.0을 **dll 1개로 합침**(Custom Interactions 별도 설치·패처 폴더 불필요). 4.1에서 이름이 바뀐 게임 코드 부분을 새 이름으로 바꾸고, 우클릭 메뉴를 4.1 구조에 맞게 다시 작성. 빌드는 통과, **인게임 동작은 미확인**.

## 무엇인가

PMC/스캐브의 **장비 겉모습만** 바꾸는 모드입니다(성능·방어력은 그대로). 인벤토리에서 아이템 우클릭 → **Transmog** 메뉴 →
"Set as PMC Armor" 처럼 고르면, 메인 메뉴·탭 메뉴·레이드 안의 내 캐릭터가 그 아이템 모습으로 보입니다. Hide(숨김)·Reset(되돌리기)도 있습니다.
대상: 방탄복·조끼·가방·안경·얼굴 가리개·헬멧·헤드셋·완장.

## 설치

1. 게임 폴더(예: `E:\SPT 4.1`) 아래 `BepInEx\plugins\Transmog\Transmog.dll` 로 넣습니다.
2. **기존 4.0용 Transmog.dll 은 지우세요**(같은 모드 ID라 둘 다 있으면 안 됩니다).
3. 원본 Custom Interactions 는 **필요 없습니다**(이 dll 안에 내장). 다른 모드가 따로 쓰고 있어도 서로 충돌하지 않게 만들었습니다.
4. 설정은 F12 → Transmog (`BepInEx\config\net.hakugyokurou.Transmog.cfg`). 스캐브에도 쓰려면 *Show Scav in transmog menu* 를 켭니다.

## 빌드

```
copy LocalSettings.props.example LocalSettings.props   (SptRoot 를 본인 SPT 경로로)
dotnet build Transmog.csproj -c Release
```
결과: `bin\Release\Transmog.dll` → 게임 폴더 자동 복사(있을 때만) + `release\Transmog-<버전>.zip`.

## 4.0 → 4.1 에서 바꾼 것 (코드 아는 분용)

| 4.0 | 4.1 |
|---|---|
| Custom Interactions + Prepatch(`DynamicInteractionClass` 개조) | 내장. `DynamicContextInteraction` 이 이미 public 이라 패처 불필요. 하위 메뉴는 `CreateDynamicContextButton` 프리픽스 + `SetSubInteractions` 로 직접 구현 |
| `LastPlayerStateClass` | `PlayerVisualRepresentation` |
| `ItemFactoryClass` / `NotificationManagerClass` | `ItemFactory` / `NotificationManager` |
| `GClass3380.smethod_2` (장비 복제) | `ItemExtensions.CloneItemInternal(…, SameIdGenerator.Instance, skipInvisibleContent: true)` |
| `HideoutPlayerOwner.method_34` | `HideSkinsForGym` |
| `TacticalClothingView` 의 `profile_1` | `_emptyProfile` (선언 순서로 추정 — 실제 프로필 `_profile` 이 아님) |
| `MatchMakerSideSelectionScreen` 의 `profile_1` | `_savageProfile` (4.1은 `_profile` 이 먼저 대입됨) |
| `ClientAppUtils.GetMainApp()` (spt-common) | `Singleton<ClientApplication<IEftSession>>.Instance` (spt-common 의존 제거) |
| `ISession` | `IEftSession` |

## 인게임에서 꼭 확인해 주세요 (미검증)

1. 인벤토리 아이템 우클릭 → Transmog 메뉴가 뜨고, 하위 메뉴(Set as / Hide / Reset)가 열리는가
2. 설정 후 메인 메뉴 캐릭터·탭(Tab) 캐릭터가 바뀌는가, 레이드 안 내 모델은 *Affect ingame player entity* 대로인가
3. 피스키퍼/레그맨 옷 가게 화면에서 변환이 **안** 걸리는가(옷 미리보기가 정상)
4. 매치메이킹 화면(진영 선택)에서 멈추지 않는가, 스캐브 변환 표시 옵션이 맞는가
5. 히드아웃 짐(운동) 중 가방을 숨겨도 멈추지 않는가

문제가 있으면 `BepInEx\LogOutput.log` 를 먼저 보내주세요.
