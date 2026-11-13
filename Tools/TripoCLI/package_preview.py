"""Package the downloaded character and locally rendered motion preview."""
import json
from pathlib import Path
import shutil
import sys

from PIL import Image, ImageDraw

root = Path(sys.argv[1])
out = root / 'Deliverables'
walk = root / 'WalkPreview'
frames = [Image.open(path).convert('RGB') for path in sorted(walk.glob('motion_*.png'))]
assert len(frames) == 20, f'Expected 20 complete frames, found {len(frames)}'
frames[0].save(out/'NoviceAdventurer_Walk.gif', save_all=True, append_images=frames[1:],
               duration=120, loop=0, optimize=False)
for source, target in [
    (walk/'NoviceAdventurer_Walk.fbx', out/'NoviceAdventurer_Walk.fbx'),
    (walk/'NoviceAdventurer_Review.blend', out/'NoviceAdventurer_Walk.blend'),
    (walk/'inspection.json', out/'walk_inspection.json'),
    (root/'RigSource/model.glb', out/'NoviceAdventurer_Rigged.glb'),
    (root/'WalkSource/model.glb', out/'NoviceAdventurer_Walk.glb')]:
    shutil.copy2(source, target)

views = [('front.png', 'Front'), ('right.png', 'Side'), ('back.png', 'Back'),
         ('threequarter.png', 'Three-quarter')]
sheet = Image.new('RGB', (1200, 840), '#202630')
draw = ImageDraw.Draw(sheet)
for i, (filename, title) in enumerate(views):
    image = Image.open(out/filename).convert('RGB').resize((300,400))
    x, y = (i % 4)*300, 24
    sheet.paste(image, (x,y))
    draw.text((x+12, 6), title, fill='white')
sheet = sheet.crop((0,0,1200,424))
sheet.save(out/'NoviceAdventurer_Views.jpg', quality=95)
report = json.loads((out/'inspection.json').read_text(encoding='utf-8'))
(out/'README.ko.md').write_text('''# Tripo Novice Adventurer

첨부 참조 이미지에서 Tripo 공식 CLI 0.8.1로 생성한 캐릭터입니다.

- 생성: H3.1 + smart_low_poly, 텍스처 포함. 40크레딧.
- 자동 리깅: rig v3.0, biped, Unity 스켈레톤 규격. 25크레딧.
- 걷기: preset:walk, 제자리 이동. 10크레딧.
- 총 사용량: 75크레딧. 리깅 사전 검사는 무료.
- 실제 메시: 22,324 삼각형, 리깅본 30,612 정점, 67개 본.
- 모든 캐릭터 정점에 스킨 웨이트가 있으며, 팔 관절 회전으로 실제 변형을 확인했습니다.
- 검, 방패, 배낭은 생성된 단일 캐릭터 메시 안에 포함되어 있습니다.
- 원본 출력 높이는 약 1 Blender 단위입니다. Unity 프로젝트에는 아직 가져오지 않았습니다.

파일:
- NoviceAdventurer_Rigged.fbx / .glb: 리깅된 캐릭터, 애니메이션 없음.
- NoviceAdventurer_Review.blend: 텍스처, 리깅, 검수용 조명과 카메라.
- NoviceAdventurer_Walk.fbx / .glb / .blend: 걷기 애니메이션 포함.
- NoviceAdventurer_Walk.gif: 실제 걷기 애니메이션의 20프레임 미리보기.
- inspection.json / walk_inspection.json: 메시, 스킨, 본, 관절 변형 검사 결과.

작업 ID:
- 모델 생성: efd79f33-3a28-48c1-be95-8b75c01ea3de
- 리깅 검사: 2ecb284c-149f-4230-8e7e-76a334df92c6
- 리깅: 9f364157-dfed-4ff4-9f54-6bc5aec4f030
- 걷기: 76068172-b78f-4f56-9ec8-a78601f86582
''', encoding='utf-8')
print(out/'NoviceAdventurer_Walk.gif')
print('Packaged', len(list(out.iterdir())), 'files')
