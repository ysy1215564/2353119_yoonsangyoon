HP 회복 아이템 박스 리소스 넣는 곳

구성 (2가지):
  1) HpBox_frames 폴더 - 박스가 열리는 애니메이션 프레임들
       파일명: hpbox_frames_01.png, hpbox_frames_02.png ... (두 자리 번호)
  2) hpitem.png - 박스가 열릴 때 튀어나와 화면 위쪽 HP 표시까지 날아가는 "아이템" 그림 1장
       (HpBox_frames 폴더가 아니라 Items 폴더 바로 아래에 위치)

규격:
  - PNG, 알파 채널 투명
  - HpBox_frames: 박스 그림을 캔버스 **아래쪽 가운데 기준**으로 그릴 것 (바닥에 놓이는 오브젝트라 이 기준으로 위치가 맞춰집니다 - Player/Monster 캐릭터와 같은 규칙, 다만 캔버스 크기 자체는 자유)
  - hpitem.png: 정가운데 기준으로 그린 아이콘 형태 권장 (원래 파일은 60x60px 정사각형 - 비슷한 비율 권장)
  - 프레임 수는 자유

넣고 나면 04_Unity_적용_가이드의 "그래픽 리소스 반영하기" 순서대로 진행하세요 (`Add HP Item Box To Scene` 도구 사용).
