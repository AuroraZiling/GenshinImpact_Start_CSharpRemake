# 梗启动

## 简介

***整个活***

基于黑屏/白屏检测的启动器
在B站看到[视频](https://www.bilibili.com/video/BV16u4y197ia/)后打算用CSharp重制

## 配置

可以在程序目录下的`config.json`修改
1. `path` 为可执行文件的目录，后缀名没有检查，其他文件的路径也行
2. `mode` 为模式，如果为`0`则是启动原神，`1`启动星铁
3. `percent` 为屏幕颜色占比阈值，有效值是`85%-100%`之间

## 感谢

[YinBuLiao/GenshinImpact_Start](https://github.com/YinBuLiao/GenshinImpact_Start)