사망(게임오버) 화면 UI 리소스 넣는 곳

파일 목록 (이 크기 그대로 유지, 디자인/그림만 자유롭게 바꿔도 됨):
  deathcut.png   사망 화면 중앙 일러스트 - 700x499px

  버튼 3종 (재도전/타이틀로/종료) - 각각 307x82px
    button_retry_normal.png / _hover.png / _click.png
    button_title_normal.png / _hover.png / _click.png
    button_quit_normal.png / _hover.png / _click.png

규칙:
  - PNG, 알파 채널 투명
  - 크기는 위 규격을 그대로 유지 (버튼은 Title/PauseMenu와 완전히 같은 307x82px 규격)
  - 크기가 달라졌으면 Image 컴포넌트의 "Set Native Size"로 원래 크기 복구 가능

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요 (`Add Death Screen To Scene` 도구 사용).
