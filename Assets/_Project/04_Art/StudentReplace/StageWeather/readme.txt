스테이지 날씨 파티클(눈/비/꽃잎 등) 리소스 넣는 곳

파일명 (정확히 이 이름으로, 소문자로만, 개수는 자유 - 늘리거나 줄여도 됨):
  weather1.png
  weather2.png
  weather3.png  (필요하면 계속 추가 가능, 코드 수정 없이 자동으로 인식됨)

파일명이 "weather"로 범용인 이유:
  지금은 눈으로 시작하지만, 나중에 비/꽃잎 등 다른 효과로 바꿀 수도 있으니
  snow1.png 처럼 효과 이름을 박아두지 않았습니다. 새 효과로 바꿀 땐 이 폴더의
  PNG만 교체하면 됩니다 (프리팹/스크립트는 그대로).

규격:
  - PNG, 알파 채널 투명 (도형 바깥쪽은 투명)
  - 여러 장을 넣을 경우 전부 같은 크기로 제작 (예: 전부 100x100px)
  - 배경/바닥과 달리 화면을 꽉 채우는 크기가 아니라, 파티클 한 개(눈송이 한 개 등)의
    모양입니다 - 작고 단순한 도형이 좋습니다

넣은 뒤 BackgroundTest 씬을 열고
"Tools > Class Template > Apply Real Stage Art" 를 실행하세요.
임포트 설정과 파티클 프리팹 연결이 자동으로 처리됩니다.
- 여러 장을 넣으면 자동으로 하나의 텍스처로 합쳐져서
  파티클 시스템의 Texture Sheet Animation에 연결됩니다.
안전하게 여러 번 다시 실행해도 됩니다.

날씨 효과가 아예 안 보이면 "Tools > Class Template > Add Weather Effect To Scene"
도 한 번 실행해 보세요.

자세한 내용은 아래 수업자료 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W05_배경_스테이지1.md
