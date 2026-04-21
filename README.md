# 🔥 OVERHEAT

> **3D 협동 멀티플레이어 열차 생존 슈터**  
> 달리는 열차 위에서 최대 4인이 몰려오는 적과 화재로부터 열차를 지키는 PVE 협동 게임

사방에서 쏟아지는 적을 협력으로 막으며 열차를 지켜야합니다.

연료로 열차 속도를 관리하며, 적의 방화를 진압해야 합니다.

상점역에서 아이템 구매, 열차 칸을 확장, 업그레이드를 해

더 멋진 열차를 꾸미고 멀리 모험을 떠날 수 있습니다.


<br>

| 기간 | 인원 | 엔진 | 네트워크 |
|------|------|------|----------|
| 2026.01.15 ~ 02.04 (21일) | 1인 | Unity 6000.2.10f1 | Photon PUN2 |

<br>

## 시연 영상

[![YouTube](https://img.shields.io/badge/YouTube-%23FF0000.svg?style=for-the-badge&logo=YouTube&logoColor=white)](https://www.youtube.com/watch?v=ZycPIp0wusM)

<br>

## 📌 목차

- [게임 소개](#-게임-소개)
- [기술 스택](#-기술-스택)
- [게임 플로우](#-게임-플로우)
- [핵심 구현](#-핵심-구현)
  - [트레드밀 맵 시스템](#1-트레드밀-맵-시스템)
  - [네트워크 풀링](#2-네트워크-풀링--ipunprefabpool--unity-objectpoolt)
  - [가짜 투사체](#3-가짜-투사체--발사--비주얼-분리)
  - [절차적 애니메이션 동기화](#4-절차적-애니메이션-동기화)
  - [로컬 무기 모션 계층 구조](#5-로컬-무기-모션-계층-구조)
  - [적 AI](#6-적-ai--navmesh-surface--link)
  - [사운드 시스템](#7-사운드-시스템)
  - [관전 카메라](#8-관전-카메라)
- [LinkedList vs List](#-LinkedList-vs-List)
- [정적 클래스 GameData](#-GameData-정적-클래스)
- [트러블슈팅](#-트러블슈팅)

<br>

---

## 🎮 게임 소개

| 핵심 요소 | 설명 |
|----------|------|
| **달리는 열차 위의 전투** | 제한된 공간에서 사방의 적을 협력으로 방어. 역할 분담이 생존의 핵심 |
| **보일러 & 화재 관리** | 연료 공급으로 속도 유지, 화재 번지기 전 진압 |
| **상점역 — 열차 확장** | 골드로 아이템 구매, 열차 칸 확장·업그레이드 |
| **최대 4인 멀티플레이** | Photon PUN2 기반 실시간 네트워크, 관전 카메라·지연보상 포함 |

<br>

---

## 🕹️ 조작

| 키 | 동작 |
|---|---|
| W, A, S, D | 이동 |
| Space | 점프 |
| Shift | 질주 |
| F | 상호작용 |
| 마우스 좌클릭 | 아이템 사용 |
| 숫자 1, 2, 3 | 퀵슬롯 선택 |
| Esc | 설정 창 열기/닫기 |

<br>

---

## ⚙️ 핵심 구현

### 1. 트레드밀 맵 시스템

**문제** PUN에서 고속 열차를 직접 동기화 시 부동소수점 오차 누적 → 미세 떨림 발산  

**해결** 열차를 `(0,0,0)` 고정 → 환경 오브젝트를 역방향 이동 (트레드밀 방식)

- 바닥: `material.mainTextureOffset` 스크롤로 이동감 표현
- 스폰/디스폰: 로컬 플레이어 위치 기준 동적 범위 + 오브젝트 풀링으로 무한 맵

> **대안** PhotonTransformView 직접 동기화 대비 — 열차 동기화 자체를 제거해 패킷 절감 & 오차 원천 차단

<br>

### 2. 네트워크 풀링 — IPunPrefabPool + Unity ObjectPool\<T\>

**문제** 적·투사체·아이템 등 빈번 생성 네트워크 객체 → GC 압박 + 패킷 처리 지연

**해결** `IPunPrefabPool` 구현으로 Unity 풀과 결합, 로컬·네트워크 단일 로직

```csharp
PhotonNetwork.PrefabPool = NetworkPool;          // IPunPrefabPool
NetworkPool.Instantiate() → PoolManager.Spawn()  // 네트워크 생성
NetworkPool.Destroy()     → PoolManager.Return() // 네트워크 파괴
```

| 측정 항목 | 적용 전 | 적용 후 |
|----------|--------|--------|
| PlayerLoop GC Alloc | 71 KB | 368 B |
| **감소율** | | **99%** |

<br>

### 3. 가짜 투사체 — 발사 / 비주얼 분리

**문제** 투사체마다 `PhotonNetwork.Instantiate` 시 패킷 폭증, 4인 교전 시 네트워크 한계  

**해결** 발사 로직과 비주얼 로직 분리

```
① MasterClient — 레이캐스트로 히트 판정 (데미지 권한 보유)
② RPC          — 발사 위치 / 방향 / 히트 포인트만 전송
③ 각 클라이언트 — 로컬 오브젝트로 비주얼(BulletTracer·이펙트)만 재현
```

→ 네트워크 투사체 생성 **0건**, 빠른 연사에서도 패킷 안정

<br>

### 4. 절차적 애니메이션 동기화

**문제** Update에서 척추 회전 시 Animator가 덮어씀 → 반영 불가  

**해결**

- `LateUpdate` — Animator 이후 척추 회전 적용
- `OnPhotonSerializeView` — 에임 각도 값만 동기화
- 수신 각도를 척추 개수로 균등 분배 + `Mathf.Lerp` 보간
- 
→ 척추 다수 동기화 → 에임 각도 동기화로 패킷 절감

<br>

### 5. 로컬 무기 모션 계층 구조

```
Player
└── CameraHolder
    └── Camera
        ├── HandSway     — 스웨이 · 걸음 흔들림 · 착지 충격
        │   └── HandHolder  — 장착 모션 · 무기 반동 회전
        └── LocalItemCamera — 로컬 아이템 레이어 전용
```

| 모션 | 구현 방식 |
|------|----------|
| `EquipMotion` | HandHolder Y축 보간 |
| `SwayMotion` | 마우스 속도 비례 오프셋 |
| `WalkMotion` | Sin 곡선 상하 흔들림, 달리기 시 진폭 증가 |
| `ShockMotion` | 착지 Y축 급강하 → 스프링 복귀 |
| `RecoilMotion` | Camera X축 회전, 무기 SO에 반동 데이터 정의 |

<br>

### 6. 적 AI — NavMesh Surface + Link

**문제** 열차 칸마다 바닥이 분리 → 기본 NavMesh로 칸 간 이동 불가  

**해결** 3단 구조

```
① 칸별 NavMeshSurface  — 런타임 Bake 1회 (트레드밀로 열차 정지 상태)
② 도어 위치 NavMeshLink — 칸 A 끝 → 칸 B 시작 자동 연결
③ Agent 연산: MasterClient만
   나머지 클라이언트: 위치·회전·Speed 수신 후 Lerp
```

→ AI 연산 비용: 클라이언트 수 무관 고정 / 동기화 3값 — Agent 경로 미전송

<br>

### 7. 사운드 시스템

**구조** SO 기반 AudioData → SoundManager (싱글톤 + AudioSource 풀)

| 구성 요소 | 내용 |
|----------|------|
| `AudioData SO` | AudioClip · 3D/2D 모드 · Volume/Pitch 범위 · 쿨다운 정의 |
| `SoundManager` | `PlaySFX(AudioData, pos)` · 디바운스 처리 · AudioMixer 볼륨 · PlayerPrefs 저장 |
| `FireSoundManager` | 화재 전용 분리 관리 |
| 적용 범위 | 무기·적·보일러·화재·열차·상점·발소리·BGM 전체 |

<br>

### 8. 관전 카메라

사망 플레이어도 팀원을 자유롭게 관전할 수 있도록 구현

| 기능 | 구현 |
|------|------|
| 자유 카메라 | `SpectatorInputAction`으로 전환 |
| 줌 | 카메라와 관전 플레이어 거리 조절 |
| AudioListener 분리 | 사망 시 로컬 비활성화 → Spectator 활성화 |
| 레이어 분리 | LocalPlayer 레이어 숨김 처리 (Layer Mask) |

<br>

---

### LinkedList vs List

| | LinkedList | **List ✅** |
|--|-----------|----------------|
| 중간 삽입·삭제 | O(1) | O(n) — 열차 수 제한으로 허용 |
| PUN 패킷 직렬화 | ❌ 불가 | ✅ int[] 변환 후 전송 |
| 인덱스 접근 | ❌ 불가 | ✅ 직관적 동기화 |
| 꼬리 자르기 | 순회 필요 | `RemoveRange()` 단번 처리 |

**결론** 패킷 직렬화 가능성 + index 기반 명확성이 결정적

<br>

### GameData 정적 클래스

**문제** `GameManager`를 `DontDestroyOnLoad` 네트워크 객체로 유지 시 씬 전환마다 ViewID 충돌  
**결론** GameManager는 씬마다 재생성, 씬 간 공유 데이터만 `static class GameData`로 분리


<br>

---

## 🐛 트러블슈팅

### #1 — 싱글톤 ViewID 충돌

| | |
|--|--|
| **원인** | `DontDestroyOnLoad` 네트워크 객체 ↔ 새 씬 오브젝트 ViewID 중복 |
| **해결** | `GameData` 정적 클래스로 분리, GameManager는 씬마다 재생성 |
| **결과** | ViewID 충돌 0건, 씬 전환 안정화 |

<br>

### #2 — 동시성 문제 (선조치 후보고)

| | |
|--|--|
| **원인** | 다수 클라이언트 동시 픽업·구매 시 RPC 지연으로 아이템 복사 |
| **해결** | 로컬 예측 선실행(선조치) → MasterClient 검증(후보고) → 플래그로 중복 무시 |
| **결과** | 복사 버그 0건, 즉각 시각 피드백 유지 |


<br>

### #3 — 비동기 로딩 중 패킷 큐 충돌

| | |
|--|--|
| **원인** | `LoadSceneAsync` 도입 시 씬 전환 중 네트워크 패킷 처리 시도 → 연결 끊김 |
| **해결** | 씬 로드 전 `IsMessageQueueRunning = false` → 완료 후 `true` 복원 |
| **결과** | 씬 전환 끊김 0건, 밀린 패킷 로드 후 정상 처리 |

<br>

### #4 — 애니메이션 레이어 충돌

| | |
|--|--|
| **원인** | 상체 Animator 레이어 추가 시 LateUpdate 척추 회전과 간섭 → 뒤틀림 |
| **해결** | 아이템 SO에 상하좌우 회전 보정값 추가, LateUpdate에서 순서 적용 |
| **결과** | 모든 아이템에서 정면 방향 보장 |

<br>

### #5 — 파티클 위치 동기화

| | |
|--|--|
| **원인** | `Update(사격) → Animator → LateUpdate(척추)` 실행 순서로 파티클 생성 위치 어긋남 |
| **해결** | 단발: `Simulation Space Local` / 지속형: `World` + LateUpdate에서 위치 매 프레임 갱신 |
| **결과** | 총구 화염·잔류 이펙트 위치 정확히 일치 |

<br>

### #6 — 씬 전환 시 NavMesh Link 고장

| | |
|--|--|
| **원인** | 씬 전환 후 열차 생성 과정에서 NavMeshLink가 잘못된 위치에 Bake |
| **해결** | 열차 생성 완료 후 NavMeshLink 갱신 / 열차 칸 파괴 시에도 재빌드 |
| **결과** | 적이 열차 칸 사이를 자연스럽게 이동 |

<br>


