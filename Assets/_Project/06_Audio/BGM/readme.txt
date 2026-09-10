스테이지1(BackgroundTest) 배경음악 넣는 곳

★ 스테이지1의 BGM만 이 폴더 "바로 아래"에 넣습니다.
  하위 폴더(Title, IntroCutscene ...)는 각각 다른 화면의 음악입니다.
  헷갈리기 쉬운 자리이니 주의하세요.

음원 파일 하나를 이 폴더에 넣으면 됨 (이름/확장자 무관 - mp3, wav, ogg 다 됨).
파일이 여러 개면 알파벳순 첫 번째만 쓰이고 경고가 뜨니, 하나만 남겨둘 것.

넣은 뒤 BackgroundTest 씬을 열고 아래를 실행하면 자동으로 연결됨.
  Tools > Class Template > Add Background Music To Scene

볼륨은 그 씬의 BackgroundMusic 오브젝트에 붙은 AudioSource에서 조절 가능.

폴더별로 어느 화면의 음악인지:
  BGM/                  스테이지1
  BGM/Title/            타이틀 화면
  BGM/IntroCutscene/    인트로 컷신
  BGM/EndingCutscene/   엔딩 컷신
  BGM/LevelClear/       스테이지 클리어 순간 (한 번만 울림)
  BGM/DeathScreen/      사망 화면 (한 번만 울림)
  BGM/Stage2/           스테이지2 (심화)
  BGM/Stage2LevelClear/ 스테이지2 클리어 (심화)
  BGM/Stage3/           스테이지3 (심화)
  BGM/Stage3LevelClear/ 스테이지3 클리어 (심화)

자세한 내용은 아래 수업자료 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W11_모바일UI_사운드.md

소리를 어디서 구하는지는 아래 문서 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/기본설명/07_사운드_구하기.md
