# 项目长期记忆


## 源码加密屏障（关键）
- 本仓库所有 .cs 文件均以 3 字节 marker `88 7d 1c` 加密；.xaml 为明文（UTF-8）可直接 Read/Edit。
- 解密由定制版 Roslyn 编译器完成（Microsoft.CodeAnalysis.CSharp.dll 内含 Decrypt 例程），`dotnet build` 透明解密并编译，故构建 0 错误；但 AI 的文件工具/Python 只读得到密文，无法读取或改写 .cs。
- 用户正常在 Visual Studio 中看到明文（IDE 解密），保存时重新加密。
- 规则：AI 只编辑 .xaml；任何 .cs 改动必须以「补丁文本」形式交用户在 VS 粘贴，或请用户运行其解密工具使文件转为明文后再编辑。绝不对加密文件做逆向/破解尝试。
