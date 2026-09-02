스테이지1 배경(패럴렉스) 리소스 넣는 곳

파일명 (정확히 이 이름으로, 소문자로만, 화면 앞->뒤 순서):
  bg1.png  (가장 앞 레이어)
  bg2.png
  bg3.png
  bg4.png
  bg5.png  (가장 뒤, 하늘 - 고정되어 안 움직임)

규격:
  - PNG, 알파 채널 투명 (배경 그림 바깥쪽은 투명)
  - 1920x1080px 기준 (화면 꽉 채우는 크기)
  - 좌우로 반복했을 때 이음새가 티 안 나게 제작
  - 공기원근(멀수록 흐리게/연하게)은 엔진에서 따로 처리 안 함 - 이미지 자체에 표현되어 있어야 함

넣은 뒤 Stage3_BackgroundTest 씬을 열고
"Tools > Class Template > Apply Real Stage Art" 를 실행하세요.
임포트 설정과 프리팹 스프라이트 교체가 자동으로 처리됩니다.
안전하게 여러 번 다시 실행해도 됩니다.

자세한 내용은 아래 수업자료 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/주차별수업/W15_최종발표_심화.md
