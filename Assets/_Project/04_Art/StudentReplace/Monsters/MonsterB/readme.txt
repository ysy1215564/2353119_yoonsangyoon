몬스터B (근접/2단 공격형) 애니메이션 프레임 넣는 곳

이 몬스터의 행동: 플레이어에게 다가와 서로 다른 공격 2종(attack1, attack2)을 상황에 따라 사용하고, 각 공격마다 전용 이펙트가 같이 나옵니다. 그림/사운드만 자유롭게 바꾸고, 행동 자체(패턴)는 그대로 재사용합니다.

폴더 목록 (총 8개):
  idle_frames             대기
  walk_frames             이동
  attack1_frames          공격 1종
  attack1_effect_frames   공격 1종에 같이 나오는 이펙트
  attack2_frames          공격 2종
  attack2_effect_frames   공격 2종에 같이 나오는 이펙트
  hit_frames              피격
  death_frames            사망

파일명 (정확히 이 규칙으로, 전부 소문자):
  monsterb_idle_01.png, monsterb_attack1_01.png, monsterb_attack1_effect_01.png ...
  (폴더 이름의 "_frames"를 뺀 부분 앞에 "monsterb_"를 붙이고 두 자리 번호)

규격 - 캐릭터 몸(idle/walk/attack1/attack2/hit/death):
  - PNG, 알파 채널 투명
  - 캔버스 500x500px 정사각형 고정, 캐릭터는 캔버스 아래쪽 가운데 기준으로 배치 (Player 폴더와 동일한 규칙)

규격 - 공격 이펙트(attack1_effect/attack2_effect):
  - PNG, 알파 채널 투명
  - 캔버스 크기는 자유 (몸 그림처럼 500x500 고정이 아님) - 다만 이펙트 그림을 **캔버스 정가운데 기준**으로 그릴 것 (Unity가 캔버스 중앙을 기준점으로 잡음). 원래 크기와 너무 다르게 만들면 화면에서 이펙트가 갑자기 커지거나 작아 보일 수 있으니, 처음 파일과 비슷한 비율로 작업하는 걸 권장합니다.

프레임 수는 폴더별로 자유입니다.

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요 (`Create Or Update MonsterB Prefab` 도구 사용).
