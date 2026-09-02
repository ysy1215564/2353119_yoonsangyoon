타이틀 화면 BGM 넣는 곳

음원 파일 하나를 이 폴더에 넣으면 됨 (이름/확장자 무관 - mp3, wav, ogg 다 됨).
파일이 여러 개면 알파벳순 첫 번째만 쓰이고 경고가 뜨니, 하나만 남겨둘 것.

넣은 뒤 Unity에서 "Tools > Class Template > Add Background Music To Title
Scene"을 실행하면 타이틀 씬의 BackgroundMusic 오브젝트에 자동으로 연결됨.
볼륨은 타이틀 씬에서 BackgroundMusic 오브젝트의 AudioSource로 직접 조절
가능 (BackgroundTest와 동일한 방식).

소리를 어디서 구하는지는 아래 문서 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/기본설명/07_사운드_구하기.md
