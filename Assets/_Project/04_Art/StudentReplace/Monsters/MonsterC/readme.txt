몬스터C (비행/원거리형) 애니메이션 프레임 넣는 곳

이 몬스터의 행동: 걷지 않고 공중에 떠서(fly) 움직이며, 돌진 공격(attack2 계열)과 투사체를 쏘는 원거리 공격을 함께 씁니다. 그림/사운드만 자유롭게 바꾸고, 행동 자체(패턴)는 그대로 재사용합니다.

폴더 목록:
  idle_frames             대기
  fly_frames              이동 (걷기가 아니라 "날아서 이동")
  attack1_frames          공격 1종 (원거리 - 아래 Effects/Projectile과 함께 사용)
  attack2_charge_frames   공격 2종의 준비/차지 동작
  attack2_dash_frames     공격 2종의 실제 돌진 동작
  hit_frames               피격
  death_frames             사망
  Effects/Projectile       공격 1종에서 실제로 날아가는 투사체(발사체) 그림

파일명 (정확히 이 규칙으로, 전부 소문자):
  monsterc_idle_01.png, monsterc_fly_01.png, monsterc_attack2_charge_01.png ...
  Effects/Projectile 안은: monsterc_projectile_01.png, monsterc_projectile_02.png ...

규격 - 캐릭터 몸(idle/fly/attack1/attack2_charge/attack2_dash/hit/death):
  - PNG, 알파 채널 투명
  - 캔버스 500x500px 정사각형 고정, 캐릭터는 캔버스 아래쪽 가운데 기준으로 배치 (Player 폴더와 동일한 규칙 - 날아다니는 몬스터라도 그림 규격 자체는 동일합니다)

규격 - 투사체(Effects/Projectile):
  - PNG, 알파 채널 투명
  - 캔버스 크기는 자유, 다만 **캔버스 정가운데 기준**으로 그릴 것 (원래 크기와 비슷한 비율 권장 - MonsterB의 공격 이펙트와 같은 규칙)

프레임 수는 폴더별로 자유입니다.

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요 (`Create Or Update MonsterC Prefab` 도구 사용).
