일시정지 메뉴 UI 리소스 넣는 곳

파일 목록 (이 크기 그대로 유지, 디자인/그림만 자유롭게 바꿔도 됨):
  pause.png   플레이 중 화면 한쪽에 항상 떠있는 일시정지(톱니바퀴) 버튼 - 122x122px, 이거 누르면 아래 메뉴가 열림

  메뉴 버튼 6종 (계속하기/조작설명/설정/포기하고 타이틀로/재도전/종료) - 각각 307x82px
    button_continue_normal.png / _hover.png / _click.png
    button_controls_normal.png / _hover.png / _click.png
    button_settings_normal.png / _hover.png / _click.png
    button_title_normal.png / _hover.png / _click.png
    button_retry_normal.png / _hover.png / _click.png
    button_quit_normal.png / _hover.png / _click.png

규칙:
  - PNG, 알파 채널 투명
  - 크기는 위 규격을 그대로 유지 (Title 화면 버튼과 완전히 같은 규격 - 307x82px)
  - 버튼 크기가 달라졌으면 Image 컴포넌트의 "Set Native Size"로 원래 크기 복구 가능

참고 - "조작설명"/"설정" 버튼을 누르면 뜨는 팝업 배경 그림은 이 폴더가 아니라 UI/Title 폴더의 popup_controls.png / popup_settings.png를 그대로 재사용합니다. 즉 Title 화면에서 그 팝업 그림을 바꾸면 일시정지 메뉴 쪽에도 자동으로 같이 반영됩니다 - 여기 따로 만들 필요 없음.

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요 (`Add Pause Menu To Scene` 도구 사용).
