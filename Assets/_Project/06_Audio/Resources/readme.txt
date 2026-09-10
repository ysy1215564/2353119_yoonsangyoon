GameAudioMixer.mixer이 들어갈 자리 (아직 없음 - 한 번만 손으로 만들면 됨)

만드는 법 (5단계, 2분 정도):
  1. 이 폴더(Resources)에서 우클릭 > Create > Audio Mixer, 이름을 정확히
     "GameAudioMixer"로 지정.
  2. Window > Audio > Audio Mixer 로 믹서 창 열기. Groups 목록에서 "Master"를
     우클릭 > Add child group, 이름을 정확히 "BGM"으로. 한 번 더 추가해서
     "SFX"로.
  3. "BGM" 그룹 선택 > 인스펙터의 Volume 슬라이더 우클릭 > "Expose 'Volume
     (of BGM)' to script".
  4. 믹서 창 우상단 "Exposed Parameters" 드롭다운을 열어서 방금 노출한 항목을
     더블클릭, 이름을 정확히 "BgmVolume"으로 변경.
  5. "SFX" 그룹도 3~4번과 동일하게: Volume 노출 -> Exposed Parameters에서
     이름을 정확히 "SfxVolume"으로 변경.

이 4개 이름(GameAudioMixer / BGM / SFX / BgmVolume / SfxVolume)은
Assets/_Project/01_Scripts/Core/GameAudioSettings.cs 코드가 그대로 찾는
이름이라 철자가 정확히 같아야 함. 다 하고 나면 "Tools > Class Template >
Add Audio Settings Bootstrap To All Scenes"를 실행해서 모든 씬에 연결.

소리를 어디서 구하는지는 아래 문서 참고.
  https://github.com/hansung-game1/game-p-docs/blob/main/기본설명/07_사운드_구하기.md
