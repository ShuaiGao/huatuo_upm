# huatuo_upm
[![license](http://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

huatuo_upm 是一个 unity 工具包的源码仓库。

huatuo_upm 是 unity中huatuo使用工具的集合，用来模拟手工安装huatuo的操作，实现自动化的安装、卸载操作。

# 安装

支持最小unity版本 2020.3

多种安装方法如下

### 方法1： 使用OpenUPM的Unity依赖文件（推荐方式）

1. 打开unity工程的根目录

2. 打开编辑文件 Packages\mainfest.json

3. 在scopedRegistries数据中添加注册信息，配置unity包搜索URL。示例如下

   ```json
   {
       "dependencies": {
           ...
       },
       "scopedRegistries": [
           {
               "name": "package.openupm.cn",
               "url": "https://package.openupm.cn",
               "scopes": [
                   "com.focus-creative-games.huatuo",
                   "com.openupm"
               ]
           }
       ]
   }
   ```

4. 切换到unity Package Manager 中将看到名为   的包，其它操作在Package Manager中进行即可。

### 方法2：使用openupm-cn命令行

关于OpenUPM CLI的命令行工具可以参照 [OpenUPM-CLI快速入门文档](https://openupm.cn/zh/docs/getting-started.html#安装openupm-cli)

1. 安装命令行工具
2. 命令行中跳转到在对应Unity工程目录（包含Assets或Packages的目录）
3. 输入命令安装`openupm-cn add com.focus-creative-games.huatuo`

### 方法3： 使用Unity Package Manager UI工具

todo

## 工作原理

### 安装和卸载

安装和卸载完全模拟手工操作，都是目录的替换。

安装流程如下：

1. 下载源代码zip。下载并将压缩包存储在缓存目录（缓存目录可配置），**如遇下载失败可手动下载并将文件置于缓存目录**。
2. 备份Libil2cpp。在il2cpp目录备份原始Libil2cpp文件夹，**此处注意在安装前应先恢复之前的本地改动**。
3. 解压缩源码zip。
4. 版本信息写入文件。版本信息写入到对应Unity Editor路径下，例：...\\2020.3.33f1c2\Editor\\.huatuo

卸载流程如下：

	1. 检查是否存在原始文件夹备份。备份文件夹名示例 例：\...\\2020.3.33f1c2\Editor\Data\il2cpp\libil2cpp_original_unity
	1. 移除libil2cpp，将libil2cpp_original_unity重命名为libil2cpp
