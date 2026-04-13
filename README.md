# Unity ECS Collision Optimization Experiment

Unity ECS(Entity Component System)를 활용하여 **10만 개의 투사체(Bullet)와 2,000개의 적(Enemy)** 사이의 충돌 연산을 최적화하는 과정을 기록한 프로젝트입니다.

## Tech Stack
- **Unity**: `6000.3.11 f1`
- **ECS Packages**: 
  - Entities (1.4.5)
  - Entities Graphics (1.4.18)
  - Unity Physics (1.4.5)
- **Core Tech**: Burst Compiler, C# Job System, Spatial Partitioning

## Optimization Journey

성능 최적화를 위해 세 가지 단계의 충돌 시스템을 비교 실험했습니다.

### 1. Worst Case (Naive Approach)
- **방식**: 모든 총알이 모든 적을 전수 조사하는 $O(m \times n)$ 방식.
- **결과**: 약 **3.54 FPS**. 연산 부하가 매우 커 실사용 불가능.

### 2. Best Case (Spatial Partitioning)
- **방식**: 공간을 격자(Grid)로 분할하여 `NativeParallelMultiHashMap`에 적 정보를 저장. 총알은 주변 9개 격자만 검사하는 $O(m + n)$ 방식.
- **결과**: 약 **73.63 FPS**. 비약적인 성능 향상 달성.

### 3. Bitwise Case (Spatial Partitioning + NativeBitArray)
- **방식**: 기존 공간 분할 방식에 `NativeBitArray`를 추가. 해시 조회를 하기 전, 해당 격자에 적이 존재하는지 비트 연산으로 선행 확인.
- **결과**: 약 **90.66 FPS**. 불필요한 해시 조회를 건너뛰어 성능을 극대화.



## Performance Comparison

| Collision System | Time Per Frame | Estimated FPS | Optimization Note |
| :--- | :--- | :--- | :--- |
| **Worst (Naive)** | 282ms | 3.54 | $O(m \times n)$ 전수 조사 |
| **Best (HashMap)** | 13.58ms | 73.63 | $O(m + n)$ 공간 분할 |
| **Bitwise (BitArray)** | **11.03ms** | **90.66** | 해시 조회 전 비트 필터링 추가 |

## Key Learnings
* **GPU Instancing & Mesh Simplification**: 10만 개 이상의 개체를 렌더링하기 위해 Sphere Mesh를 Cube로 교체하고 GPU Instancing을 활용하여 Draw Call 및 삼각형 수를 98% 이상 절감했습니다.
* **Structural Changes**: `foreach` 루프 내 직접적인 `DestroyEntity`는 구조적 변경을 일으키므로, **ECB(Entity Command Buffer)**를 사용하여 루프 종료 후 일괄 처리하도록 설계했습니다.
* **Bitwise Filtering**: 데이터 밀도가 낮은 광활한 공간에서는 해시 조회 비용조차 아끼기 위해 비트 배열을 필터로 사용하는 것이 효과적임을 확인했습니다.

---

### Demo Video
[![ECS Build Video](https://img.youtube.com/vi/pQRCIHdseh8/0.jpg)](https://youtu.be/pQRCIHdseh8?si=AKEtAZwTHcyrmg6T)
*(이미지를 클릭하면 유튜브 영상으로 이동합니다.)*
