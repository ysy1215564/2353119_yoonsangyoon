타이틀 화면 UI 리소스 넣는 곳

규격(이 크기 그대로 유지, 디자인/그림만 자유롭게 바꿔도 됨):
  메인 버튼 5종 (시작하기/조작설명/설정/크레딧/종료) - 각각 307x82px
    button_start_normal.png / _hover.png / _click.png
    button_controls_normal.png / _hover.png / _click.png
    button_settings_normal.png / _hover.png / _click.png
    button_credits_normal.png / _hover.png / _click.png
    button_quit_normal.png / _hover.png / _click.png
  설정 팝업의 저장후닫기 버튼 - 241x80px (상태 이미지 1장만 씀, 정상)
    button_settings_save.png
  팝업 배경 그림 3종 - 각각 602x715px (조작설명/설정/크레딧 안내문이 그림 안에 텍스트로 들어감)
    popup_controls.png / popup_settings.png / popup_credits.png
  슬라이더(BGM/효과음 공용, 두 슬라이더가 같은 그림을 재사용함)
    slider_track.png - 258x15px
    slider_handle.png - 18x53px
  전체 화면 배경/워터마크 - 각각 1920x1080px
    title_background.png (타이틀 로고 텍스트까지 포함해서 한 장으로, 애니메이션 프레임들 가장 뒤에 깔리는 정적 배경)
    title_watermark.png (학교로고+트랙로고+하단 텍스트, 항상 최상단 고정 오버레이라 학생이 수정하는 대상 아님)

  타이틀 배경 애니메이션 프레임 - 각각 1920x1080px, title_background.png 바로 위에서 반복 재생됨
  (버튼 클릭/팝업과 무관하게 타이틀 화면에 있는 동안 계속 반복)
    title_motion01.png, title_motion02.png, title_motion03.png ...
    - 순서대로 두 자리 숫자로 번호를 매길 것 (01, 02 ... 09, 10, 11 ...)
    - 프레임 수는 자유롭게 늘리거나 줄여도 됨 - 파일 넣고 나면
      "Tools > Class Template > Refresh Title Motion Frames" 실행
    - 재생 속도는 TitleMotion 오브젝트의 Title Background Motion Player
      컴포넌트에서 Frames Per Second 값으로 조절 (기본 5fps, 느긋한 느낌)

규칙:
  - PNG, 알파 채널 투명 (배경 바깥쪽은 투명)
  - 파일명은 위에 적힌 그대로 정확히 맞출 것 (대소문자까지)
  - 크기는 위 규격을 그대로 유지 - 디자인이 달라져도 픽셀 크기는 같아야 함.
    작업하다 실수로 크기가 달라졌으면, Unity에서 그 버튼 오브젝트의
    Image 컴포넌트 > "Set Native Size" 버튼 한 번 누르면 원래 크기로
    돌아옴(위치는 안 바뀜).
  - 버튼 "위치"는 자유 - Scene 뷰에서 원하는 자리로 옮겨도 됨 (그림+클릭
    영역이 같이 붙어 다니는 구조라 따로 맞출 것 없음).

그림을 같은 파일명으로 덮어쓰면 Unity가 자동으로 반영합니다.
임포트 설정도 자동으로 처리되므로 따로 할 것이 없습니다.

title_motion 프레임 장수를 바꿨을 때만 아래를 실행하세요.
  Tools > Class Template > Refresh Title Motion Frames

★ "Tools > Class Template > Create Title Scene" 은 실행하지 마세요.
  타이틀 씬을 처음부터 다시 만들기 때문에, 옮겨둔 버튼 위치와
  조정한 값이 전부 초기화됩니다. 최후의 수단으로만 쓰는 명령입니다.

자세한 내용은 아래 수업자료 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W07_타이틀화면.md
