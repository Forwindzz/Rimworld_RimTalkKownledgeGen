# RimTalk_GenKnowledge

## 中文简介
`RimTalk_GenKnowledge` 是一个给 RimWorld 使用的常识生成 Mod。  
它会在游戏内读取 Def 数据，按可配置规则生成可被记忆系统使用的“常识”条目（Tag + Content + Importance）。

## English Overview
`RimTalk_GenKnowledge` is a RimWorld mod for automatic knowledge generation.  
It reads in-game Def data and generates configurable knowledge entries (Tag + Content + Importance) for memory/knowledge systems.

## 中文功能
- 在已加载存档中手动一键生成常识（不会在主菜单运行）。
- 支持多种 Def 处理器（如 `XenotypeDef`、`ThingDef`、`GeneDef`、`ResearchProjectDef`、`RecipeDef`、`HediffDef`、`FactionDef`、`MemeDef`、`PawnKindDef`、`TraitDef`）。
- 每类处理器可独立配置：
  - Tag 模板
  - Knowledge 模板
  - Importance 基础值与上下限
  - 该类 Def 的专属筛选和权重规则
- 提供全局过滤能力：
  - 最小重要性阈值
  - 跳过名单（现实常识 / 高重复概念）
- 支持全局“显示数值”开关（关闭后尽量只输出程度描述）。
- 支持 Debug 信息附加（processorId / logicalKey / defName / modPackageId）。
- 集成 Memory UI Patch（可在设置中开关），在常识界面快速触发：
  - 根据 Defs 生成
  - 打开 Defs 配置面板
- 中英文语言支持。

## English Features
- One-click manual generation in a loaded save (not in main menu).
- Multiple Def processors are supported (e.g. `XenotypeDef`, `ThingDef`, `GeneDef`, `ResearchProjectDef`, `RecipeDef`, `HediffDef`, `FactionDef`, `MemeDef`, `PawnKindDef`, `TraitDef`).
- Per-processor configuration:
  - Tag template
  - Knowledge template
  - Base/min/max importance
  - Processor-specific filters and weighting rules
- Global filtering:
  - Minimum importance threshold
  - Skip lists (real-world common concepts / high-redundancy concepts)
- Global numeric-display toggle (off = preference for tendency-only descriptions).
- Optional debug prefix output (processorId / logicalKey / defName / modPackageId).
- Memory UI Patch integration (toggle in settings) with quick actions:
  - Generate from Defs
  - Open Defs config panel
- Bilingual localization (Chinese/English).

## 快速使用 / Quick Start
- 打开 Mod 设置页，调整全局与各处理器配置。
- 进入存档后，点击“生成常识（当前存档）”。
- 如需清理历史生成结果，点击“清理已生成常识”。

