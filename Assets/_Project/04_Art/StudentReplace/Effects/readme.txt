플레이어 공격/필살기에 같이 나오는 이펙트(검격, 폭발, 레이저 등) 넣는 곳
(몬스터 자체 공격 이펙트는 여기가 아니라 각 Monsters/MonsterX 폴더 안에 따로 있습니다)

폴더 목록 - 어떤 플레이어 동작과 짝지어지는지:
  AttackEffect01_frames        기본 공격 1타 (Player/attack1_frames)에 같이 나오는 이펙트
  AttackEffect02_frames        기본 공격 2타 (Player/attack2_frames)에 같이 나오는 이펙트
  JumpAttackEffect_frames      공중 공격 (Player/jump_attack_frames)에 같이 나오는 이펙트
  SpecialChargeEffect_frames   필살기 차지 중 (Player/special_charge_frames)에 같이 나오는 이펙트
  SpecialLaser_frames          필살기 발동(Player/special_fire_frames) 중 나가는 레이저
  SpecialGroundBurst_frames    필살기 발동(Player/special_fire_frames) 중 바닥에서 터지는 이펙트
                                 (필살기 발동 동작 하나에 레이저+바닥폭발 두 이펙트가 같이 쓰입니다)

파일명 (정확히 이 규칙으로, 전부 소문자):
  effect_attack1_01.png, effect_attack1_02.png ...
  effect_attack2_01.png / effect_jump_attack_01.png / effect_special_charge_01.png /
  effect_special_laser_01.png / effect_special_ground_burst_01.png (전부 두 자리 번호)

규격:
  - PNG, 알파 채널 투명
  - 캐릭터 그림(Player, Monsters)과 달리 **캔버스 크기가 고정되어 있지 않습니다** - 이펙트 크기만큼 자유롭게 캔버스를 잡으면 됩니다.
  - 대신 그림을 **캔버스 정가운데 기준**으로 그릴 것 (Unity가 캔버스 중앙을 이펙트의 기준점으로 잡습니다 - 좌우/상하로 안 치우치게)
  - 원래 파일과 크기 비율이 많이 달라지면 게임 화면에서 이펙트가 갑자기 커지거나 작아 보일 수 있습니다 - 처음 파일 크기와 비슷한 비율로 작업하는 걸 권장합니다.
  - 프레임 수는 폴더별로 자유

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요.
