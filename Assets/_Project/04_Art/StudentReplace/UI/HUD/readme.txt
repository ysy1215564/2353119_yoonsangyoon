플레이 중 화면에 항상 떠있는 HP(체력)/스페셜 게이지 UI 리소스 넣는 곳
(일시정지 버튼 그림은 여기가 아니라 UI/PauseMenu/pause.png 입니다)

파일 목록 (이 크기 그대로 유지, 디자인/그림만 자유롭게 바꿔도 됨):
  hp_background.png   HP 표시 전체 배경판 - 591x155px
  hp_dot_black.png     HP 1칸(채워진 상태) - 35x35px, 정사각형 권장
  hp_dot_white.png     HP 1칸(빈 상태) - 35x35px, 정사각형 권장
  sp_gauge_frame.png   스페셜 게이지 테두리(항상 보임) - 326x66px
  sp_gauge_full.png    스페셜 게이지가 다 찼을 때 안쪽에 채워지는 그림 - 326x66px

규칙:
  - PNG, 알파 채널 투명
  - 크기는 위 규격을 그대로 유지 (달라지면 Image 컴포넌트의 "Set Native Size"로 원래 크기 복구 가능 - 위치는 안 바뀜)
  - hp_dot_black/white는 HP가 몇 칸인지에 따라 이 그림이 옆으로 여러 개 나열되는 방식입니다 (칸 하나 = 이 그림 한 장)

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요.
