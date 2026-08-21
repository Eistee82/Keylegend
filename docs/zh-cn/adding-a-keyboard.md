# 添加或纠正键盘

对某块键盘的支持是**数据，不是代码**。你不需要 C#，也不需要构建工具——一个文本编辑器和你自己的键
盘就够了。

来到这里的人多半无需添加什么，因为适合他们布局的配置已经存在。这些配置缺的是唯一无法生成的东西：
有人拿着硬件确认每个键都在配置所声称的位置亮起。**那就是[第 2 部分](#2-纠正一份配置)描述的工作，
大约十分钟。**

---

## 一份配置知道什么，又有多确定

一份配置回答两个彼此独立的问题，而它们的可靠程度并不相同：

| 问题 | 答案从何而来 | 有多确定 |
|---|---|---|
| 每个键在哪里，多大？ | 自 IBM Model M 以来每块键盘都遵循的 19.05 毫米标准栅格 | **确定。** 几何由布局推出。 |
| 哪个 LED 矩阵单元点亮该键？ | 厂商公布的矩阵，且假定是一块标准键盘 | **推测。** 各型号会挪动按键、留下未装配的单元，也会添加自己的键。 |

这一分野正是 `verified` 标志存在的全部理由。标为 `"verified": false` 的配置几乎肯定画对了图，而
很可能弄错了哪个键会亮。

---

## 1. 补上缺失的布局

先确认它是否真的缺失：`devices/` 里已经有 ANSI-US、ISO-DE、ISO-UK、ISO-FR、ISO-ES、ISO-IT、
ISO-NORDIC、ISO-PT、ISO-CH、ISO-RU、ISO-PL、JIS-JP 和 ABNT2-BR 的全尺寸配置，另有 tenkeyless、
75 %、65 % 与 60 % 变体。若你的就在其中，请直接跳到第 2 部分。

### 生成的路子

`tools/make-layout.py` 依据标准尺寸构建配置。往里加一块键盘，就是文件末尾 `PROFILES` 列表中的一
个条目：

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| 参数 | 决定什么 |
|---|---|
| `form_factor` | `fullsize`、`tkl`、`75`、`65`、`60`、`fullsize-macro` |
| `variant` | `ansi`、`iso`、`jis` 或 `abnt2`——回车键的形状，以及有哪些额外按键 |
| `legends` | 采用哪一套印刷键帽字样：`en`、`de`、`fr`、`es`、`it` |
| `right` | `win` 或 `fn`——右 Alt 与菜单键之间是什么 |

然后运行：

```bash
python tools/make-layout.py --only iso-tr
```

如果你的键盘字样不在这五套之中，就加一套：在同一文件中复制 `LEGENDS_EN`，翻译其中条目，再登记到
`LEGEND_SETS`。只有*什么都不打*的键才需要字样——其余的在运行时向 Windows 询问，而这正是一份配置
能在同一硬件上服务于所有软件布局的原因。

### 手写的路子

若某块键盘不是标准布局的变体——正交排列、分体式、带着别人都没有的一排宏键——就直接写 `device.json`。
[格式说明](device-profile-format.md)列出了每个字段，而 `devices/device-profile.schema.json` 能给
多数编辑器提供补全和行内报错。

第一遍不必精确。把按键放得大差不差，凡是拿不准的地方就把 `row` 和 `column` 留作 `null`，其余交给
校准。

---

## 2. 纠正一份配置

这是需要硬件的部分，也是真正要紧的部分。

### 先看一眼

在动键盘之前，先看看图：

```bash
python tools/preview-layout.py devices/generic-fullsize-ansi-us/device.json
```

这会在配置旁边写出 `preview.svg`；用任意浏览器打开它。把它与面前的键盘对照，留意：

- 缺失的按键，或画出了你键盘上没有的按键
- 形状不对的回车键——ISO 上是高而呈 L 形，ANSI 上是宽而扁平
- 修饰键数量不对的底排，这一处的差异比其他任何地方都大
- **红色轮廓**，它标记的是没有矩阵单元的按键。那些永远不会亮。

修正几何是算术，不是猜谜：栅格是每键一个单位，而一个单位就是普通字母键所具有的 `width`。

### 然后校准

校准每次点亮一个键并报出它的名字，好让你确认发白光的那个键正是配置所声称的键。只有这样才能确定；
其余一切都是从厂商表格推出来的。

```bash
keylegend-cli --profile devices/<你的目录>/device.json --calibrate
```

它按阅读顺序走完已映射的按键：

| 按键 | 作用 |
|---|---|
| `Enter` 或 `→` | 这个对，继续下一个 |
| `F` | 亮错了键——记下来 |
| `←` | 退回一个键 |
| `A` | 同时点亮所有已映射的按键 |
| `S` | 直接跳到汇总 |
| `Q` 或 `Esc` | 停止 |

由于按键 id 沿用美式布局，提示还会显示该键在*你的*机器上实际打出什么——所以在德语键盘上告诉你的是
“ß 键”，而不是 `Keyboard_MinusAndUnderscore`。

发现会随手写入 `calibration-findings.txt`，而不是等到最后。校准是需要耐心的活儿，关掉窗口不该让你
白干。

工作时另一张图很有用——它给每个键标注其所声称的矩阵单元，而不是键帽字样：

```bash
python tools/preview-layout.py devices/<你的目录>/device.json --cells
```

### 应用你的发现

`tools/apply-calibration.ps1` 会把发现写回配置，并保留一份 `.bak` 副本：

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<你的目录>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` 用于什么都没点亮的按键：矩阵能寻址该单元，但这款型号那里没有 LED。这类按键保留几何——键
毕竟存在，预览也应当画出它——并失去 `row`/`column`，免得往虚空里发送东西。`-Remap` 用于映射到错误
单元的按键。

### 该预料到什么

以下是生成的配置最常出错的地方：

| 何处 | 会发生什么 |
|---|---|
| **ISO 回车键** | 它跨两个单元。许多键盘只有下面那个装了 LED，上半部由邻居照亮，或者根本不亮。 |
| **底排** | 修饰键的数量和宽度因型号而异。游戏键盘把 `Fn` 放在办公键盘摆第二个 Windows 键的位置。 |
| **宏键与多媒体键** | 常在第 0 列或最外侧的列上，而且常常不在任何单元上。 |
| **紧凑键盘** | 矩阵仍保持完整的 6 × 22；60 % 的键盘只是把其中大部分留空。单元不会重新编号。 |
| **小键盘的高键** | 加号和回车跨两行，却只听命于一个单元——通常是上面那个。 |

事实证明没有 LED 的按键保留几何、失去单元：

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

它仍会被画出来，因此预览与硬件相符；它只是永远不亮。这是正确的，不是缺陷。

### 标记为已验证

当每个单元都对上之后，给同一个脚本传 `-MarkVerified`，或者手动写上 `"verified": true`，并删掉那条
说明配置是生成的 `note`。这个标志正是在告诉下一个用你这块键盘的人：可以信任它。

---

## 3. 测试

```bash
dotnet test
```

随附配置的测试会校验 `devices/` 下的每一份配置，也包括你的。它们能抓出重复的 id、两个键争抢同一颗
LED、彼此叠画的按键、越出矩阵的单元，以及滑出画布的几何。

## 4. 发起 pull request

请说明你核对的是哪块键盘、哪种物理布局，以及是否走完了校准。参见
[CONTRIBUTING.md](../../CONTRIBUTING.md)。

`"verified": false` 的配置同样受欢迎——它们为下一个拥有该键盘的人提供了起点。对现有配置的一处纠
正，与一份全新配置同样有价值。

### 关于图片

`image` 字段是可选的，目前并未使用：预览由几何绘制，因此在任何尺寸下都保持清晰，也不可能与配置相
矛盾。如果你仍要附上一张，那必须是**你**自己拍摄或绘制的。厂商的产品渲染图无法在本项目的 MIT 许可
下发布，带有此类图片的 pull request 会被要求移除。

## 另见

- [设备配置格式](device-profile-format.md) —— 逐个字段详解
- [架构](architecture.md) —— 为什么按键含义来自 Windows 而不是一张表
