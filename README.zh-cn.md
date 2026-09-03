# Keylegend

**面向 Razer Chroma 的交互式键盘灯光——按键按其此刻的实际作用发光。**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **版本 1.2.0。** 灯光、界面、游戏检测和应用程序配置均已可用。
> [下载安装程序或免安装版](https://github.com/Eistee82/Keylegend/releases/latest)，也可以从源码构建。
> 参见 [CHANGELOG.md](CHANGELOG.md)。

![Keylegend 按每个键此刻的含义为其着色，并在前台程序切换时随之更换配置](docs/images/keylegend.png)

---

## 它做什么

大多数 RGB 软件把键盘当作装饰。Keylegend 把它当作**显示器**。

每个按键的颜色取决于它*此刻*的含义——含义一变，颜色随即改变：

- **锁定状态一目了然。** Num Lock、Caps Lock 和 Scroll Lock 在键帽上直接显示自己的状态。
- **按字符类别着色。** 数字、小写字母、大写字母、符号和控制键各有自己的颜色。
- **按住修饰键，就能看到那一层。** 按下 `AltGr`，只有真正带有 AltGr 字符的键才继续亮着。按下
  `Windows`，Windows 快捷键便按功能分组亮起。`Alt`、`Ctrl` 及其组合同理。
- **Shift 和 Caps Lock 自然生效。** 由于每个键产生什么字符是实时向 Windows 查询的，字母会自行
  从“小写”色切换到“大写”色。Num Lock 关闭时，小键盘会改为导航配色。
- **游戏另有一套。** 游戏会被自动识别——包括无边框窗口模式——WASD、其周围的按键和数字行会采用
  固定颜色：游戏时重要的是手放在哪里，而不是某个键打出哪个字母。
- **按应用程序区分的配置，随附约九十个。** Photoshop、Visual Studio Code、Excel、Elden Ring
  等等，只要程序获得焦点就立即生效；指名程序的配置优先于通用游戏配置。修改其中一个，只有被你
  改动的那部分不再跟随随附版本，其余部分仍会随后续版本一同改进。
- **灯光可以回应打字。** 八种效果可选，默认为*无*：被按下的键淡出再回来、闪光或余晖不散，
  一滴水或一道暗波掠过整块键盘，周围的键随之震动，火花四溅，或者按键随使用而升温、再慢慢冷却。
  效果叠在颜色之上，而不是混进颜色里——每个键仍然表示它原本的含义。
- **它会交还灯光。** 经过可设置的空闲时间（默认 60 秒）后，Keylegend 释放键盘，让你的
  Chroma Studio 效果重新接管。
- **十一种语言。** 英语、德语、西班牙语、法语、意大利语、荷兰语、波兰语、葡萄牙语、俄语、
  乌克兰语和简体中文。界面跟随 Windows 的显示语言，也可在设置中切换。键帽字样不受影响：它们
  跟随你的键盘，而不是菜单。

由于按键含义来自**当前生效的 Windows 键盘布局**而非写死的表格，Keylegend 无需改动即可配合任何
布局——中文、德语、美式、Dvorak 都可以。

## 工作原理

Keylegend 向 Windows 询问在当前键盘状态下每个键会产生什么字符（`ToUnicodeEx`），由该字符推出
类别，再通过本地 REST 接口把得到的颜色表发送给 Razer Chroma SDK。

它有意**不**安装全局键盘钩子。它读取的是*状态*——某个键此刻是否按下——从不拦截、
转发或记录任何击键。未选打字效果时，它只看修饰键和锁定键的状态；选了效果，它会另外询问这块键盘的哪些键正被按下，
仅此而已。
参见 [docs/zh-cn/architecture.md](docs/zh-cn/architecture.md)。

## 环境要求

- Windows 10 或 11
- Razer Synapse，且 Chroma SDK 服务正在运行
- 一块已连接的 Razer Chroma 键盘（见下）
- .NET 10 运行时

## 安装

```powershell
winget install Eistee82.Keylegend
```

这是最省事的方式：winget 会把 .NET 运行时作为已声明的依赖一并取来，无需手动安装任何前置组件。否则
就挑一个文件：

[**下载最新版本。**](https://github.com/Eistee82/Keylegend/releases/latest)

| 文件 | 是什么 |
|---|---|
| `Keylegend-1.2.0-setup.exe` | 为当前用户安装，无需管理员权限。会创建开始菜单项；卸载时一并移除开机启动项。 |
| `Keylegend-1.2.0-portable.zip` | 同一个程序，解压即用。请把语言文件夹（`de`、`fr` 等）留在可执行文件旁边，否则界面会回退为英文。 |

两者均未签名，因此 Windows 会提示发布者未知——证书的年费高于本项目所能负担。每个版本都附带
`SHA256SUMS.txt` 供校验下载，生成它的构建日志也是公开的。

## 支持的键盘

**任何 Razer Chroma 键盘。** 没有列表，也没有按型号划分的文件，因为 Keylegend 不需要识别你的键盘——它直接
询问。Razer Synapse 会描述已连接的键盘：型号名称、以数字表示的物理布局，以及硬件实际拥有的按键。Razer
为该型号绘制的图形提供其余部分——真实的按键尺寸、带滚轮和多媒体键的外壳，以及键帽上印刷字符的轮廓，语言正确。

图形唯一没有说明的，是每个按键属于灯光矩阵的哪个单元。那是 Chroma 协议的常量，在所有型号上都相同——这也是
Synapse 本身同样不需要按型号建表的原因。对照唯一一台手工校准过的键盘检验：全部 105 个按键一致。

**物理布局**描述键盘的*形状*，而不是你输入的语言。某个键产生什么字符是在运行时向 Windows 询问的，
因此即使 Windows 设为 US 或 Dvorak，德语键盘仍能正确工作。

**需要 Razer Synapse**，已安装并正在运行，且键盘已连接。键盘的描述来自那里，它的图形也存放在那里。

## 文档

| 主题 | |
|---|---|
| 架构 | 着色如何决定，以及为什么没有键盘钩子 |
| 添加配置 | 按应用程序着色 |
| 配置 | 设置、设置文件、开机启动 |

提供十一种语言：

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

英文和德文是持续维护的原本；凡译文与之不符，以英文为准。欢迎提交更正，参见
[CONTRIBUTING.md](CONTRIBUTING.md)。

## 构建与运行

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd Keylegend
dotnet build
dotnet test
```

`Keylegend.exe`（`src/Keylegend.App`）就是整个程序：窗口、通知区域图标、设置。
唯一值得知道的开关是 `--verify`：它检查一份副本是否带着随附配置和全部十一种语言，
把结果写入紧随其后给出的路径，并通过退出码作答。发行脚本就是用它检查打好包的副本。

设置位于 `%APPDATA%\Keylegend\settings.json`，由应用程序写入。

## 参与贡献

错误报告、应用程序配置和翻译都很受欢迎——参见 [CONTRIBUTING.md](CONTRIBUTING.md) 和
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)。

## 许可

[MIT](LICENSE)。两个第三方捐助按钮除外；此处不含任何厂商的代码、头文件、库或美术素材——参见
[NOTICE.md](NOTICE.md)。

## 商标声明

本项目**与 Razer Inc. 无关，未获其背书或赞助。**

RAZER 与 RAZER CHROMA 是 Razer Inc. 的商标或注册商标。此处使用它们，仅为指明本项目所配合的硬件
和软件接口，属于指称性使用所允许的范围。Keylegend 是一个由社区维护的独立项目。

本仓库中的其他名称同理。应用程序与游戏配置提到了约九十个程序——Photoshop、Visual Studio Code、
Excel、Elden Ring 等——文档提到了键盘厂商与型号。它们是各自权利人的商标，出现在此仅为说明
某项内容对应哪个程序或哪块键盘。Keylegend 与它们均无关联，也不含它们的代码或素材。参见
[NOTICE.md](NOTICE.md)。
