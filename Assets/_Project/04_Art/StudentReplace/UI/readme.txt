화면 인터페이스(UI) 그림 넣는 곳

  Title/          타이틀 화면 - 배경, 버튼 5종, 팝업 3종, 슬라이더
  HUD/            플레이 중 항상 보이는 것 - HP 표시, 필살기 게이지
  PauseMenu/      톱니 버튼 + 일시정지 메뉴 버튼 6종
  DeathScreen/    사망 화면 일러스트 + 버튼 3종
  StageStart/     스테이지 시작 배너
  StageClear/     스테이지 클리어 배너
  MobileControls/ 모바일 터치 조작 (조이스틱, 버튼 4종)

각 폴더 안의 readme.txt 에 파일명과 픽셀 크기가 적혀 있습니다.

★ UI는 픽셀 크기를 반드시 지켜야 합니다
  캐릭터 그림과 달리 정해진 크기가 있습니다. 크기가 달라지면 화면에서
  찌그러지거나 잘립니다. 실수로 달라졌다면 Unity에서 그 오브젝트를 선택하고
  Inspector의 Image 컴포넌트에서 "Set Native Size" 를 누르면 복구됩니다.

★ 버튼은 3장이 한 세트입니다
  _normal(평소) / _hover(마우스를 올렸을 때) / _click(눌렀을 때).
  셋 다 같은 크기여야 합니다.

자세한 내용은 아래 수업자료 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W06_플레이HUD_메뉴.md
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W07_타이틀화면.md
