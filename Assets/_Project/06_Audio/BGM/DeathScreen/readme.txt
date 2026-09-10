사망 화면(이미지+메뉴)이 뜨는 순간 1번만 재생되는 사운드 넣는 곳

파일 이름은 반드시 "deathscreen_bgm"으로 시작해야 함 (소문자로만, 확장자는 mp3/wav/ogg 등 무관,
예: deathscreen_bgm.mp3). 다른 이름이면 자동으로 못 찾음.

넣은 뒤 Unity에서 ActionTest.unity, BackgroundTest.unity 씬을 각각 열고
"Tools > Class Template > Add Death Screen To Scene"을 실행하면
DeathScreenController 오브젝트의 AudioSource에 자동으로 연결됨.
루프 안 되고 죽음 화면이 뜨는 순간 1번만 재생됨(PlayOneShot).
볼륨은 DeathScreenController 오브젝트의 AudioSource에서 직접 조절 가능.

소리를 어디서 구하는지는 아래 문서 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/기본설명/07_사운드_구하기.md
