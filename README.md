# MarqueeLight

屏幕边缘跑马灯灯带，与 OpenCode AI 编程助手联动，通过灯带的**颜色**和**动画模式**直观展示 AI 的会话状态。

## 效果预览

| 状态 | 灯带效果 |
|---|---|
| AI 运行中 | 🟢 绿色跑马灯（流动） |
| AI 运行完成 | 🟢 绿色常亮 |
| 需要用户输入 | 🔴 红色闪烁（渐入渐出） |
| 发生错误 | 🔴 红色闪烁 |
| 无会话 / 程序关闭 | 关闭灯带 |

## 快速开始

### 1. 编译并启动灯带

```bash
cd MarqueeLight
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

运行：

```bash
./publish/MarqueeLight.exe
```

程序启动后，灯带默认**不显示**，直到 OpenCode 插件首次调用 API。

### 2. 安装 OpenCode 插件

```bash
# 将插件文件复制到 OpenCode 全局插件目录
cp opencode-plugin/notification.js ~/.config/opencode/plugins/notification.js
```

重启 OpenCode 后插件自动生效。

### 3. 使用

保持 `MarqueeLight.exe` 在后台运行。当 OpenCode 中 AI 开始工作时，灯带自动亮起并切换状态。

## HTTP API

灯带通过本机 HTTP API 控制（端口 `50080`）：

| 方法 | 路径 | 说明 |
|---|---|---|
| `POST` | `/color` | 设置颜色 `{"color":"red\|yellow\|green\|blue"}` |
| `POST` | `/mode` | 设置模式 `{"mode":"steady\|marquee\|blink"}` |
| `POST` | `/speed` | 设置速度 `{"speed":0.1~5.0}` |
| `POST` | `/start` | 开始动画 |
| `POST` | `/stop` | 停止动画 |
| `GET` | `/status` | 获取当前状态 |

### 状态映射

插件自动将 OpenCode 事件映射为灯带状态：

```js
状态 → 灯带:
  busy (运行中)  → 绿色 + marquee (跑马灯)
  idle (完成)    → 绿色 + steady (常亮)
  input (需输入) → 红色 + blink (闪烁)
  error (错误)   → 红色 + blink (闪烁)
  closed (关闭)  → 关闭灯带
```

## 托盘菜单

系统托盘中可以手动：

- 切换颜色主题（红色/黄色/绿色/蓝色）
- 显示/隐藏灯带
- 退出程序

## 配置

`appsettings.json` 可调整默认参数：

- `Color` — 初始颜色
- `Speed` — 跑马灯速度
- `GlobalOpacity` — 全局透明度
- `LightLengthRatio` — 跑马灯光带长度比例
- `LightWidth` — 灯带宽度（像素）
- `HttpPort` — HTTP API 端口

## 项目结构

```
MarqueeLight/
├── MarqueeLight/          # C# WinForms 项目源码
│   ├── LightEngine.cs     # 灯带渲染引擎
│   ├── MainForm.cs        # 透明窗体
│   ├── HttpServer.cs      # HTTP API 服务
│   ├── TrayManager.cs     # 系统托盘
│   └── Models/            # 数据模型
├── opencode-plugin/       # OpenCode 集成插件
│   └── notification.js
├── publish/               # 编译输出
└── README.md
```
