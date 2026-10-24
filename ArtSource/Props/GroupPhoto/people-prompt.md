# Group Photo 人物生成 prompt

交给 Codex 生图，在场景渲染图上加入十二名新职工。定稿是在 Claude Code 里通过 Codex MCP 的 `codex_image` 生成的，参数 `model` gpt-6-astra、`reasoning_effort` high、`size` 16:9、`count` 3。生成结果放到 `Generated/`，贴回原渲染、黑白做旧、白边和日期由 `Scripts/` 里的脚本完成。

## 上传的图

`Background/GroupPhoto_Background_1920.png`（1920 × 1080）。需要更高分辨率时用同目录的 `gate_d16_y5_f42.png`（3840 × 2160），两张画面相同。

## Prompt

```text
Edit the attached image. It is a photograph of the gate of a coal washing plant in Shitanjing, Ningxia, in northwest China, taken on 3 October 1980. Keep the whole image exactly as it is: the same framing, the same 16:9 aspect ratio, the same camera position and lens, the same gate, brick gate pillars, guardhouse, three-storey dormitory, road, mountains, sky, light and colours. Keep the sky the same hazy mauve-grey; do not make it bluer. Do not move, redraw, restyle or remove anything that is already there. Leave the red banner and its hand-painted Chinese characters completely untouched.

Add only this: exactly twelve newly hired young workers posing for their commemorative group photo on the bare dirt ground in front of the open gate, centred under the banner.

Arrangement and size (this matters most; earlier attempts made the people far too big and too close):
- Exactly twelve people: six standing in the back row and six squatting on their heels in the front row, about one metre in front of the back row. Not eleven, not thirteen.
- The group stands about 12 to 13 metres from the camera, a few metres in front of the gate. The camera is at standing eye height, so the heads of the standing men are level with the horizon: their heads are at the height of the middle horizontal rail of the two open gate leaves, far below the banner, leaving a wide band of buildings and road visible between the banner and their heads.
- In a 1920 x 1080 frame: each standing man is only about 185 pixels tall, from the top of his head at about y 590 to his feet at about y 770. The squatting men's heads are at about y 670. The whole group is only about 400 pixels wide, roughly from x 630 to x 1030, i.e. about as wide as the middle half of the banner, and stays between the two gate pillars without covering either gate leaf.
- The people occupy only a small area in the lower middle of the picture; most of the frame remains the empty scene. They should look small and distant, as in a wide shot that shows the whole gate.
- Feet stand on the dirt with soft contact shadows that match the direction and softness of the existing light.

People:
- Twelve clearly different individuals: young Chinese men aged about 18 to 26, ordinary workers from northwest China, lean builds, sun-tanned faces, varied face shapes and heights, short neat haircuts of 1980, a few wearing plain cloth caps.
- Clothes typical of October 1980: navy blue and grey cotton Zhongshan-style jackets and work jackets, a few faded army-green jackets without any insignia, one or two white shirt collars showing, dark trousers, black cloth shoes or green canvas rubber-soled shoes. Clean but worn and slightly wrinkled, not costume-like.
- Natural, slightly stiff poses of people not used to being photographed: arms at their sides or hands clasped in front, one or two with a hand on a neighbour's shoulder. Serious or shy faces, a couple of faint smiles, all looking toward the camera. Correct hands, no extra limbs or faces.

Look:
- Photorealistic, like a real photograph taken at eye level with a medium-format film camera in natural daylight. Keep the colours and tones of the attached image and output a clean colour image.
- No film grain, scratches, sepia, black-and-white conversion, borders, captions, dates, watermarks or any text. These are added later.
- Add nothing else: no other people, vehicles, flags, slogans, badges, armbands, red paper flowers, plants, grass or weeds, signs or litter.

Output one image with the same framing and 16:9 aspect ratio, 1920 x 1080 or larger.

Before finishing, look at every generated image and count the people. Regenerate any image that does not have exactly twelve people, or where the standing men's heads are above the gate leaves' middle rail or the group is wider than the middle half of the banner.
```

## 可选的一句

想让十二人里有两名女工，在 People 一段末尾加上：

```text
Two of the twelve are young women in the same kind of work jackets, one with short bobbed hair and one with two braids.
```

## 挑图时看

- 正好十二个人，两排，脚踩在地上，没有多出来的手和脸。
- 人头在横幅下方，横幅整条露出来。横幅上的字被改坏没关系，贴回时横幅一带全用原渲染，只要人没有挡住横幅。
- 人通常会比 prompt 写的大，整组在两扇门之间、没有盖住门扇就能用。
- 大门、门柱、门卫室、宿舍楼的位置和原图一致，没有加杂草和别的东西。背景颜色变了没关系，贴回时只取人物。
