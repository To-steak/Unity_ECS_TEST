# Unity_ECS_TEST

Worst 충돌 시스템: O(M * N) 시간복잡도를 가지며 Enemy와 Bullet을 매 프레임마다 비교한다.   
Best 충돌 시스템: O(M + N) 시간복잡도를 가지며 공간 분할을 통해 Bullet 근처의 9개의 격자 내 Enemy에 대해서만 비교한다.   
Bit 충돌 시스템: Best 충돌 시스템에서 Bit 배열을 추가한 버전이다. Bit 배열은 적이 있는 Index에 값을 1로 표기한다. 이후 Bullet과 Enemy를 비교할 때, Bit 배열의 값이 0이면 계산을 하지 않는다.   
