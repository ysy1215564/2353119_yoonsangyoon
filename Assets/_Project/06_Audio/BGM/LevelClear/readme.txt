스테이지를 클리어하는 순간 1번만 재생되는 사운드 넣는 곳

파일 이름은 반드시 "level_clear_bgm" 으로 시작해야 함 (소문자로만,
확장자는 mp3/wav/ogg 등 무관, 예: level_clear_bgm.mp3).
다른 이름이면 자동으로 못 찾음.

★ 반복되는 배경음악이 아니라 "짠!" 하고 한 번 울리는 소리입니다.
  길게 만들 필요 없습니다 (2~5초 정도).
  이 소리가 울리는 동안 스테이지 BGM은 자동으로 멈춥니다.

넣은 뒤 BackgroundTest 씬을 열고 아래를 실행하면
StageClearController 오브젝트의 AudioSource에 자동으로 연결됨.
  Tools > Class Template > Add All HUD & Systems To Background Test Scene

소리를 어디서 구하는지는 아래 문서 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/기본설명/07_사운드_구하기.md
