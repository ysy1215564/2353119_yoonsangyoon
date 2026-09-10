몬스터B 효과음 넣는 곳

파일명 (정확히 이 이름으로, 소문자로만):
  monsterb_attack1   공격 1
  monsterb_attack2   공격 2
  monsterb_hit       맞았을 때
  monsterb_death     죽었을 때

확장자는 자유 (wav / mp3 / ogg 다 됨). 이름만 맞으면 인식됨.
같은 이름으로 확장자만 다른 파일이 둘 이상 있으면 경고가 뜨고 알파벳순
첫 번째가 쓰이니, 바꿀 때는 예전 파일을 지울 것.

넣은 뒤 Unity에서 아래를 순서대로 실행할 것.
  1) Tools > Class Template > Wire MonsterB SFX Clips
  2) Tools > Class Template > Apply Monster B Settings To Prefab

★ 2번을 빼먹으면 ActionTest 씬에서는 소리가 나는데
  BackgroundTest(스테이지) 씬에서는 안 납니다. 제일 많이 빠뜨리는 단계입니다.

자세한 내용은 아래 수업자료 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W11_모바일UI_사운드.md

소리를 어디서 구하는지는 아래 문서 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/기본설명/07_사운드_구하기.md
