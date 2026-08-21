# 设备配置格式

一份设备配置描述一种物理布局下的一款键盘型号。它是 `devices/` 下某个目录中的单个文件，目录名为
`<厂商>-<型号>-<布局>`：

```
devices/razer-deathstalker-v2-de/
└── device.json     几何与 LED 对应关系
```

`devices/device-profile.schema.json` 以机器可读的形式描述同样的内容。像随附配置那样在 `$schema`
行里指明它，多数编辑器便能在你输入时给出补全和行内报错。

## device.json

```jsonc
{
  "$schema": "../device-profile.schema.json",
  "formatVersion": 1,
  "name": "Razer DeathStalker V2",
  "vendor": "Razer",
  "model": "DeathStalker V2",
  "physicalLayout": "ISO-DE",
  "canvas":  { "width": 439.5, "height": 135.5 },
  "matrix":  { "rows": 6, "columns": 22 },
  "verified": true,
  "keys": [
    { "id": "Keyboard_Escape", "x": 6, "y": 6, "width": 19, "height": 19,
      "row": 0, "column": 1, "label": "esc" }
  ]
}
```

| 字段 | 含义 |
|---|---|
| `formatVersion` | 格式修订号。目前为 `1`。构建会拒绝编号高于它所理解的配置。 |
| `name` | 界面上显示的名称。 |
| `vendor`、`model` | 谁制造的、哪个型号。描述布局而非产品的配置用 `"Generic"`。 |
| `physicalLayout` | `ANSI-US`、`ISO-DE`、`JIS-JP`、`ABNT2-BR` …… —— 按键的物理*排布*，而不是软件布局。 |
| `canvas` | 所有按键位置所参照的坐标系。只有比例有意义；随附配置以毫米计。 |
| `matrix` | 厂商 LED 矩阵的大小。Razer 键盘无论尺寸大小都是 6 × 22。 |
| `verified` | 当有人在真实硬件上确认过对应关系后为 `true`。 |
| `note` | 可选的自由文本，写给下一个打开此文件的人。 |
| `image` | 可选，且目前未使用 —— 见下文[图片](#图片)。 |
| `keys[]` | 每个键一条。 |

### 物理布局，不是软件布局

`physicalLayout` 决定键盘的*形状*：回车键是否高而呈 L 形、`Z` 左侧是否有一个额外的键、底排是否带
着日语转换键。

它对这些键产生什么字符只字未提。那是 Keylegend 在运行时向 Windows 询问的，针对当前生效的布局。因
此一份 ANSI-US 配置无论 Windows 设为中文、美式还是 Dvorak 都同样适用——这也正是为什么按*物理*布局
各有一份配置，而不是按语言。

### 按键条目

| 字段 | 含义 |
|---|---|
| `id` | 唯一标识。沿用既有命名：`Keyboard_A`、`Keyboard_Enter`、`Keyboard_NonUsBackslash`、`Keyboard_Num7`。 |
| `x`、`y` | 左上角在画布上的位置。 |
| `width`、`height` | 按键在画布上的尺寸。 |
| `row`、`column` | 厂商 LED 矩阵中的单元。未知时二者皆为 `null` —— 这是有效状态，也正是校准的用武之地。 |
| `scanCode` | 覆盖标准扫描码。仅在物理布局与美式命名相抵触之处才需要。 |
| `parts` | 属于同一个键的其他矩形，用于非矩形的按键。 |
| `label` | 键帽上印着什么，用于不打出任何字符的键。 |
| `labelSecondary` | 第二行印字，位于第一行下方。 |

### 键帽字样属于键盘

`label` 是*印在键帽上的东西*，而不是对该键功能的翻译。德语键盘写 `strg`，法语键盘写 `ctrl`，意大
利语键盘写 `bloc maiusc`——而且无论 Keylegend 自己的菜单设为哪种语言，它们都照写不误。切换界面语
言绝不会改变键帽字样。

产生字符的键根本不带 `label`。它们的字样来自当前生效的 Windows 布局，因而会自行跟随 Shift、
Caps Lock 和 AltGr。

### 多于一个矩形的按键

ISO 回车键是标准情形：一个键跨两行。

```jsonc
{
  "id": "Keyboard_Enter",
  "x": 267.25, "y": 72.5, "width": 23.75, "height": 19,
  "row": 3, "column": 14,
  "scanCode": 28,
  "parts": [ { "x": 262.5, "y": 53.5, "width": 28.5, "height": 19 } ],
  "label": "enter"
}
```

主矩形承载着单元，`parts` 补足形状的其余部分。之所以写明 `scanCode`，是因为上半部占据的正是 ANSI
用作反斜杠的位置：没有它，回车键的上部就会被当作打出 `\` 来着色。

### 仅存在于某一种布局的按键的扫描码

`Keylegend.Core` 中的标准表覆盖的是美式键盘所拥有的按键。只在别处存在的键在配置中写明自己的代码，
这样就不必为了某个布局去改 C#：

| 标识 | 按键 | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | JIS 上退格键左侧的 `¥` | `0x7D` |
| `Keyboard_JpRo` | JIS 上右 Shift 右侧的 `ろ` | `0x73` |
| `Keyboard_JpMuhenkan` | 空格键左侧的 `無変換` | `0x7B` |
| `Keyboard_JpHenkan` | 空格键右侧的 `変換` | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | ABNT-2 上右 Shift 右侧的 `/?` 键 | `0x73` |

## 校验器强制执行的规则

这些会在持续集成中检查，因此违反它们的配置无法合入：

- 按键 id 唯一
- 没有两个键争抢同一个矩阵单元
- 没有两个键在画布上重叠
- `row` 与 `column` 要么都填，要么都是 `null`
- 单元落在所声明的矩阵之内
- 按键落在画布之内
- 每个键的尺寸为正
- `image` 所指名的图片确实存在

## 命名与 ISO/ANSI 的差异

按键 id 沿用美式布局，因为厂商自己的矩阵就是这么做的。在德语键盘上，物理的 `Z` 因此落在
`Keyboard_Y` 上，反之亦然。这只关乎名称：位置和行为都不受影响，因为实际字符是在运行时向 Windows
询问的。

有两个标识只存在于 ISO 键盘：

| 标识 | 按键 | Razer 单元 |
|---|---|---|
| `Keyboard_NonUsBackslash` | `Y`/`Z` 左侧的额外按键（`<`、`>`、`\|`） | `RZKEY_EUR_2`，第 4 行第 2 列 |
| `Keyboard_NonUsTilde` | 主排上紧挨回车键的那个键（`#`、`'`） | `RZKEY_EUR_1`，第 3 行第 13 列 |

在 ISO 键盘上，高回车键跨两个矩阵位置：上半部位于 ANSI 摆放反斜杠之处（第 2 行第 14 列），下半部
位于 `Keyboard_Enter`（第 3 行第 14 列）。

**两者是否真的都会亮，取决于型号。** 厂商表格描述的是矩阵能够*寻址*什么，而不是某块键盘实际*装配*
了什么。在 DeathStalker V2 上，校准显示上面那个单元根本不驱动任何 LED——整个回车键都由下面那个点
亮，这也正是随附配置把回车建模为一个带两个矩形的键、而不是两个键的原因。

这恰恰是任何文档都推不出来的东西，也正是一份配置在有人于硬件上逐键走过之前不该标为 `verified` 的
理由。

## 图片

`image` 是可选的，目前并未使用：屏幕上的预览由上面的几何绘制而成。绘制能让预览在任何窗口尺寸下都
保持清晰，也使得图片与配置不可能彼此矛盾。

如果你仍要附上一张，那必须是**你**自己拍摄或制作的图片。整个仓库以 MIT 许可发布，该许可赋予任何人
修改和再分发其内容的权利——而对键盘厂商的产品摄影，谁都无权作此授予。参见
[NOTICE.md](../../NOTICE.md)。

## 另见

- [添加或纠正键盘](adding-a-keyboard.md) —— 实际操作流程
