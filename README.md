# Neon Rush：霓虹轨道

三轨道 3D 无尽跑酷 Demo。玩家在赛博风格跑道上自动前进，通过左右换道、跳跃、下滑躲避障碍物，收集能量并通过能量门；能量充满后可以释放能量爆发，清理前方障碍物。项目重点是完整玩法闭环和图形学展示：URP、Bloom、发光材质、后处理、粒子特效、能量门、跑道光效和赛博环境搭建。

## 推荐环境

- Unity：Unity 6 `6000.1.17f1`
- 模板：Universal 3D / 3D URP
- 渲染管线：Universal Render Pipeline
- 输入：Unity Input System
- 目标平台：Windows PC 演示
- 主场景：`Assets/Scenes/Main.unity`

## 核心玩法

- 三轨道自动向前跑酷
- A/D 或左右方向键换道
- Space/W 跳跃
- S 或下方向键下滑
- 障碍物、收集物、能量门
- 分数、能量条、开始界面、失败界面、重新开始
- 赛博跑道材质、Bloom、粒子反馈、能量爆发
- 背景音乐由场景中的 `MusicManager` 播放

## 当前目录结构

```text
Assets/
  AllSkyFree/                 天空盒素材
  Animations/                 角色动作素材
  Art/                        项目美术资源
  Audio/
    Music/                    背景音乐
  Materials/                  材质资源
  Naxida/                     导入的 3D 角色模型
  Prefabs/
    TrackSegments/            跑道段、障碍物、收集物等组合 prefab
    VFX/                      粒子特效 prefab
  Scenes/                     Unity 场景
  Scripts/
    Core/                     游戏流程、分数等核心逻辑
    Gameplay/                 障碍物、收集物、能量门、能量爆发
    Graphics/                 图形效果和运行时视觉辅助
    Player/                   玩家控制、动画桥接、跟随光效
    Track/                    跑道生成与路段逻辑
    UI/                       UI 管理
  Settings/                   URP、Volume、渲染设置
  Shades/                     Shader 文件
  StarterAssets/              Unity Starter Assets 资源
  ThirdParty/                 第三方资源
docs/
  asset_sources.md            外部素材来源与版权记录
  graphics_notes.md           图形效果说明
  team_tasks.md               组员分工
```

## 主场景对象

- `GameManager`
- `TrackSpawner`
- `Player`
- `MusicManager`
- `Main Camera`
- `Directional Light`
- `Global Volume`
- `Canvas`
- `EventSystem`

## 组员分工摘要

详细分工见 `docs/team_tasks.md`。

- 同学 1（队长）：负责游戏主要逻辑、项目整合、赛博整体风格、后处理、环境模型搭建、渲染包导入、游戏音效和 3D 角色模型导入。
- 同学 2：负责跑道部分，包括跑道段、障碍物、收集物制作，以及它们在跑道上的摆放和可玩性调整。
- 同学 3：负责能量爆发特效、游戏开始界面、跑道光效，以及相关视觉展示内容。

## 素材来源

项目中使用或参考的网络/第三方来源已记录在 `docs/asset_sources.md`，主要包括：

- 3D 角色模型：APlayBox 上的纳西妲/Nahida 相关模型资源；纳西妲角色版权归《原神》及米哈游 miHoYo/HoYoverse 所有，相关角色设计、模型设计、贴图和知识产权归原权利方所有。
- 赛博环境模型：Quaternius Ultimate Platformer Pack / Cyberpunk Game Kit，项目内记录为 CC0 1.0 Universal / Public Domain Dedication。
- 基础动画与部分音效资源：Unity Starter Assets - Third Person Character Controller，遵循 Unity Companion License。
- 天空盒：AllSky Free - Deep Dusk，来自 Unity Asset Store。
- 渲染/风格相关包：DELTation Toon Shader，MIT License。
- 视觉参考：Delt06 URP Toon Shader Cyberpunk Demo，用作赛博 Bloom、色彩和后处理方向参考，未直接导入其 Demo 场景文件。
- 背景音乐：`Assets/Audio/Music/FH6_You.mp3`，标注为《极限竞速：地平线 6 / Forza Horizon 6》相关音乐素材，仅用于课堂学习和非商业演示。

## 版权与免责声明

本项目仅用于课程学习、课堂展示和非商业 Demo 演示，不用于销售、公开发行或商业运营。

项目中的角色模型、音乐、第三方美术包、Unity 示例资源和其他外部素材，其版权、商标权、角色设计、模型设计、贴图、音乐作品及相关知识产权均归原作者、发行方或相应权利人所有。本项目不声明拥有这些外部素材的版权。项目中的纳西妲/Nahida 角色来自《原神》相关角色形象，角色版权归《原神》及米哈游 miHoYo/HoYoverse 所有，本项目仅作为学习展示使用。

其中背景音乐 `FH6_You.mp3` 标注为《极限竞速：地平线 6 / Forza Horizon 6》相关音乐素材，版权归其原权利方所有；本项目只在学习用途下作为背景音乐测试和课堂展示使用。请勿将该音乐或项目中的外部素材用于商业用途、二次售卖、公开分发或任何违反原授权条款的用途。

如果后续要公开发布项目，请替换为明确允许商用或公开发布的原创/授权音乐与模型，并重新核对 `docs/asset_sources.md` 中所有素材的授权状态。
