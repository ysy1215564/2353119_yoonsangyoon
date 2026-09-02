모바일(Android) 터치 컨트롤 UI 리소스 넣는 곳
(PC/Windows 빌드에는 안 보이고, 모바일로 빌드했을 때만 화면에 나오는 버튼들입니다 - 기본 과제는 아니고 시간이 남아 모바일 빌드까지 도전할 때 필요)

파일 목록 (이 크기 그대로 유지, 디자인/그림만 자유롭게 바꿔도 됨):
  joystick_frame.png   왼쪽 하단 이동용 조이스틱 - 바깥 테두리 - 331x331px
  joystick_stick.png   이동용 조이스틱 - 손가락으로 미는 안쪽 손잡이 - 331x331px
  jump_button_normal.png / jump_button_pressed.png       점프 버튼 - 222x222px
  attack_button_normal.png / attack_button_pressed.png   공격 버튼 - 222x222px
  dash_button_normal.png / dash_button_pressed.png       대시 버튼 - 222x222px
  attack_special_button_normal.png / attack_special_button_pressed.png   필살기 버튼 - 222x222px

규칙:
  - PNG, 알파 채널 투명
  - 각 버튼은 상태 2가지만 있음(normal/pressed) - 터치 화면은 마우스 hover 개념이 없어서 Title/HUD 버튼(3상태)과 다름
  - 크기는 위 규격을 그대로 유지 (달라졌으면 Image 컴포넌트의 "Set Native Size"로 복구 가능)

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요 (`Add Mobile Controls To Scene` 도구 사용).
