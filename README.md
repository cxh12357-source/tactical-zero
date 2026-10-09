# Tactical Zero / 战术禁区

一个通过 AI 编程助手协同迭代的游戏原型项目，公开用于展示需求拆解、试玩反馈、功能验证与代码实现。

- **web/**：可运行的 HTML5 Canvas 4.0 原型，JavaScript + Node.js 22 + WebSocket。包含单机建筑清剿、四座多楼层建筑、听声定位、武器选择、清敌后三秒撤离；旧模式保留 2–4 人局域网/同源 WebSocket 对战和职业系统。
- **unity/**：Unity 6000.6.0f1、C#、URP、NavMesh 的 3D 0.18.0 源码快照，包含脚本、编辑器工具、测试、设置及少量有明确授权的资源。最新玩法包含四座建筑、15 层、13 把原枪、NPC跨层导航和门墙遮挡。

两个目录是不同实现。Unity 快照为控制体积排除了完整场景和大型生成模型，**不能直接构建成与本地完整工程相同的游戏**。可运行入口是 web 原型；仓库不包含已发布 Windows 可执行文件或完整 WebGL 构建包。

## 项目角色与 AI 辅助方式

项目发起者提出玩法、HUD、开镜、复杂地图、清剿及撤离需求，实际试玩并反馈穿墙、NPC穿楼板、操作与运行异常。AI 编程助手协助生成/修改代码、资源工具、回归检查和构建流程。这里公开真实产出，不宣称项目全部代码由个人手写，也不宣称训练了大模型或强化学习算法。NPC行为采用游戏规则与导航系统。

## 启动 Web 原型

安装 Node.js 22 或更新版本，然后运行：

```sh
cd web
npm ci --ignore-scripts
npm run check
npm run validate:maps
npm test
npm start
```

浏览器打开 http://localhost:4173/ 。停止终端会停止服务。若端口被占用，可通过 PORT 环境变量更改。首次离线缓存与安装 PWA 需要 localhost 或 HTTPS；联机需要运行 Node 服务，静态网页托管不会提供 WebSocket 对战。

`npm test` 会先生成当前单文件手机版和 iPhone 缓存包；这些生成文件不纳入版本控制。单机资源已在 web/dist 中，生成单文件版可运行：

```sh
cd web
node scripts/build-mobile.cjs
```

产物位于 web/mobile/build/game.html 。触控与 Safari 真机表现仍需实机验证。

## Unity 源码阅读

使用 Unity Hub 安装 6000.6.0f1，打开 unity 目录；包依赖由 Package Manager 获取。主要代码：

- Assets/TacticalZero/Scripts/World：建筑布局、任务、碰撞、声音与撤离。
- Assets/TacticalZero/Scripts/AI：NPC状态与导航。
- Assets/TacticalZero/Scripts/UI：菜单、HUD、触控。
- Assets/TacticalZero/Tests：NUnit / Unity回归检查。
- Assets/Plugins/WebGL：网页音频与PWA桥接。
- Tools/Blender：本项目的模型生成/导出工具，依赖 Blender；所用 MakeHuman 图形基础网格为 CC0。

当前快照缺少完整场景、材质、模型与音效，不能据此承诺编辑器PlayMode测试或完整构建可复现。历史验证摘要保存在 docs/validation-summary.json ，明确区别完整原工程的测试和此公开快照的验证。

## 已有验证与限制

完整原 Unity 0.18.0 工程形成 23 项通过的回归记录及 750 次出生点检查；NPC穿楼板复现中的实体天花板相交由 19 帧变为 0 帧。WebGL离线加载、双窗口更新、音频方向及声部清理有历史记录。本公开副本已通过Web语法/控件检查、四图15层共120出生区连通验证、18个文件组成的完整测试套件和手机版重建；实际启动服务后，健康检查、首页及19个脚本资源均返回HTTP 200。

真实 iPhone 连续对局、真人耳机试听及长期运行尚未验证；各地图性能改善不一致。用户后来报告过一次 Unity 着色器阶段闪退，兼容模式测试未证明彻底修复。不开出移动端帧率或“完全稳定”的保证。

## 许可

项目自有源代码按 [MIT](LICENSE) 发布。原始/AI辅助生成的 Web 项目插图与图标见 [资源许可](ASSET_LICENSE.md)。MakeHuman 基础网格为 CC0；Noto 字体为 OFL，不能统一改为MIT。第三方来源与完整声明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

仓库不包含原远程配置、托管身份、运行日志、账号、存档、node_modules、Unity Library 或构建缓存。
