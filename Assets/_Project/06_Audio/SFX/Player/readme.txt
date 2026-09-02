주인공 효과음 넣는 곳

파일명 (정확히 이 이름으로, 소문자로만):
  player_attack1_swing        공격 1타 휘두르는 소리
  player_attack2_swing        공격 2타 휘두르는 소리
  player_jump_attack          점프 공격
  player_special_charge_loop  필살기 차징 중 (누르고 있는 동안 반복 재생)
  player_special_fire         필살기 발사
  player_jump                 점프
  player_dash                 대시
  player_hit                  맞았을 때
  player_heavy_hit            크게 맞았을 때(넘어짐)
  player_death                죽었을 때

확장자는 자유 (wav / mp3 / ogg 다 됨). 이름만 맞으면 인식됨.
같은 이름으로 확장자만 다른 파일이 둘 이상 있으면 경고가 뜨고 알파벳순
첫 번째가 쓰이니, 바꿀 때는 예전 파일을 지울 것.

넣은 뒤 Unity에서 아래를 실행하면 주인공에 자동으로 연결됨.
  Tools > Class Template > Wire Player SFX Clips

★ player_special_charge_loop 만 성격이 다름
  누르고 있는 동안 계속 반복되는 소리라, 앞뒤가 자연스럽게 이어지는
  (끊김 없이 반복되는) 소리로 만들 것. 다른 것은 전부 한 번만 재생됨.

소리가 몇 번째 그림에서 나는지는 주인공 Inspector의
Attack 1 Swing Frame / Attack 2 Swing Frame 이 정함.
프레임 장수를 바꿨다면 그 숫자도 확인할 것.

자세한 내용은 아래 수업자료 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W11_모바일UI_사운드.md

소리를 어디서 구하는지는 아래 문서 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/기본설명/07_사운드_구하기.md
