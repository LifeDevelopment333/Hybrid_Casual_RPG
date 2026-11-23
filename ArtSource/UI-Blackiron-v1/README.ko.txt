A안: 흑철과 잿불 — UI 그래픽 스프라이트 v1
제작일: 2026-11-24

출력 위치: Assets/RPG/ART/UI/Blackiron-v1
총 46개 투명 RGBA PNG. 각 파일은 Unity Sprite (2D and UI), Single로 임포트했다.

구성
- panels: 6개. 메인/구역/능력치 행/아이템 상세 패널, 초상화 프레임, 제목 장식.
- controls: 16개. 버튼 일반/강조/눌림/비활성, 슬롯 일반/희귀/영웅/선택,
  상태바 트랙/HP 채움/EXP 채움/적 HP 채움, 조이스틱 바탕/손잡이, 메뉴 탭 일반/선택.
- icons: 16개. 전투/장비/가방/능력치/HP/공격/방어/이동/공격속도/치명타/
  추가/초기화/닫기/뒤로/골드/자동전투.
- equipment: 8개. 대검/가죽 두건/가죽 갑옷/장갑/바지/반지/목걸이/귀걸이.

사용
1. 모든 PNG는 Sprite 모드로 설정되어 Image 컴포넌트의 Source Image에 바로 지정할 수 있다.
2. panels 6개와 버튼 4개, 슬롯 4개, bar-track, 탭 2개(총 17개)는 경계가 설정되어 있다.
   UI Image Type을 Sliced로 설정하면 경계를 보존하며 늘릴 수 있다.
   장식이 보존되는 최소 표시 크기는 각 파일의 경계 합보다 커야 한다.
3. HP/EXP/적 HP 채움은 별도 이미지다. Image Type=Filled, Fill Method=Horizontal,
   Origin=Left로 두고 Fill Amount에 값을 연결한다. 트랙과 채움의 RectTransform은 각각 맞춘다.
4. 아이콘/아이템 그림/조이스틱은 Image Type=Simple, Preserve Aspect를 사용한다.
5. frame-portrait의 가운데는 투명하다. 캐릭터 초상화를 아래 레이어에 별도로 둔다.
6. 글자/숫자는 PNG에 없다. TextMeshPro로 제목/설명/HP/경험치/버튼 문구를 올린다.
7. 일반/강조/눌림/비활성 버튼은 Sprite Swap으로 사용할 수 있다.
   상태별 원본 크기에 약간의 차이가 있어 같은 RectTransform과 Sliced 설정으로 맞춘다.

임포트 설정
- PPU 100, 중심 Pivot, Full Rect Mesh
- Alpha Is Transparency 활성, Mip Map 비활성
- Clamp, Bilinear, Uncompressed, NPOT 스케일 없음
- .meta는 Unity API로 생성/설정했다. 수동 편집하지 않았다.

제작 방식과 파일
- A 시안을 참고해 built-in image_gen으로 텍스트 없는 투명 부품을 재제작했다.
  시안 픽셀을 그대로 잘라낸 결과가 아니므로 장식과 재질 표현에 작은 차이가 있다.
- 원본 시트 4장: Sheets/*.png
- 생성 프롬프트와 구성: generation-spec.json
- 개별 파일 크기, 원본 좌표, 9-slice 경계: sprite-manifest.json
- 분리/임포트 도구: Tools/UIBlackiron/ExportSprites.cs
- 도구는 기존 PNG 덮어쓰기를 차단한다. Main(false)는 PNG를 변경하지 않고 확인 및 목록 기록만 한다.
- 출력 폴더는 이번 작업에서 새로 만들었다. 되돌리려면 새 Blackiron-v1 폴더를 Unity에서 제거하면 된다.
- 실제 게임 씬과 기존 UI, 캐릭터, 전투 코드는 변경하지 않았다.

검증
- 46개 Sprite 로드 성공, PNG/스프라이트 크기 일치, 17개 경계 일치.
- 실제 RGBA 알파 존재, 분리 범위에 투명 여백 존재, 눈에 보이는 부품의 셀 경계 잘림 없음.
- 생성 시트의 패널 행 간격 차이를 확인하고 빈 간격으로 절단 위치를 조정했다.
- 컴파일 실패 없음. 최종 임포트 검증 이후 추가 콘솔 오류 없음.
- 별도로 Frontier 환경 프리팹의 Missing Nested Prefab 로그 9건이 확인됐다.
  해당 환경 에셋은 수정하지 않았다. 프로젝트 전체 오류가 해결됐다는 의미는 아니다.

참고 문서
- 설치된 2D Sprite 패키지 Documentation~/DataProvider.md
- Unity 6000.3 TextureImporter.textureType, Texture2D.GetPixels32, ImageConversion.EncodeToPNG
- ISpriteFrameEditCapability API: 경계 편집 가능 여부 확인 후 적용.

이 결과는 v1 그래픽 부품 세트다. 실제 기기 크기에서의 화면 배치와 상호작용 연결은 별도 작업이다.
