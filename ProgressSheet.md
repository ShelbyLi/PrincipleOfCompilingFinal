### 运行

##### 解释器

```shell
# 编译解释器 interpc.exe 命令行程序 
dotnet restore  interpc.fsproj   # 可选
dotnet clean  interpc.fsproj     # 可选
dotnet build -v n interpc.fsproj # 构建./bin/Debug/net5.0/interpc.exe ，-v n查看详细生成过程


# 执行解释器
# ./bin/Debug/net5.0/interpc.exe ex1.c 8
# dotnet run -p interpc.fsproj ex1.c 8
dotnet run -p interpc.fsproj .\example\myex1.c 3
# dotnet run -p interpc.fsproj -g ex1.c 8  //显示token AST 等调试信息
dotnet run -p interpc.fsproj -g .\example\myex1.c 3

=============================================
dotnet restore  interpc.fsproj
dotnet clean  interpc.fsproj
dotnet build -v n interpc.fsproj
dotnet run -p interpc.fsproj .\example\myex1.c 3

```

##### 编译器

```shell
# 构建 microc.exe 编译器程序 
dotnet restore  microc.fsproj # 可选
dotnet clean  microc.fsproj   # 可选
dotnet build  microc.fsproj   # 构建 ./bin/Debug/net5.0/microc.exe


# 编译
dotnet run -p microc.fsproj .\example\myex1.c 3    # 执行编译器，编译 ex1.c，并输出  ex1.out 文件
dotnet run -p microc.fsproj -g .\example\myex1.c 3   # -g 查看调试信息

# 运行 (↑说要在vm中运行)
gcc machine.c -o machine  # 第一次运行即可
.\machine.exe .\example\myex1.out 3

====================================
dotnet restore  microc.fsproj
dotnet clean  microc.fsproj
dotnet build  microc.fsproj
dotnet run -p microc.fsproj .\example\myex1.c 3
gcc machine.c -o machine
.\machine.exe .\example\myex1.out 3


======================================Contcomp
dotnet restore  microc.fsproj
dotnet clean  microcc.fsproj
dotnet build  microcc.fsproj
dotnet run -p microcc.fsproj .\example\myex1.c 3
.\machine.exe .\example\myex1.out 3
```

##### 进度

| 实现                               | 测试     | Interp         | Comp                                   | ContComp   | 备注                                        |
| ---------------------------------- | -------- | -------------- | -------------------------------------- | ---------- | ------------------------------------------- |
| `++e; --e`                         | myex1.c  | ✔              | ✔                                      | ✔          | Exercise8.3                                 |
| `e1?e2:e3`                         | myex2.c  | ✔              | ✔                                      | ✔          | Exercise8.5                                 |
| `for (i=0; i<n; ++i)`              | myex3.c  | ✔              | ✔                                      | ✔          |                                             |
| `e++; e--`                         | myex1.c  | ✔              | ✔                                      | ✔          |                                             |
| `+=; -=; *=; /=; %=`               | myex4.c  | ✔              | ✔                                      | ✔          |                                             |
| `do while`                         | myex5.c  | ✔              | ✔                                      | ✔          |                                             |
| `switch case` (无break)            | myex6.c  | ✔              | ✔                                      | ✔          |                                             |
| `printf` (未检查类型)              | myex7.c  | ✔              |                                        |            |                                             |
| `char`                             | myex8.c  | ✔              | ✔                                      | ✔          |                                             |
| `break continue`                   | myex9.c  |                | ✔                                      |            | 编译continue for i++不执行                  |
| `int i=0`                          | myex10.c | ✔ (全局未实现) | ✔ (全局未实现)                         | ✔          |                                             |
| `float`                            | myex11.c | ✔              | ✔                                      | ✔          |                                             |
| `String` 及相应的`printf`          | myex12.c | ✔              |                                        |            | 长度固定128                                 |
| `int(x) char(x)`                   | myex13.c | ✔              | 要判断返回的值大小再决定类型<br />how? |            | 判断变量的类型:<br />>100000000 判断为float |
| `struct`(string array不能做member) | myex14.c | ✔ TOFIX        | ✔                                      | ✔          |                                             |
| `&; |; <<; >>; ^; ~`               | myex15.c | ✔              | ✔                                      | ✔          |                                             |
| `bin oct hex`                      | myex16.c | ✔              | ✔                                      | ✔          | TOFIX 只支持输入                            |
| `bool`                             | myex17.c | ✔              | ✔                                      | ✔          |                                             |
| 标识符定义 可`_`开头               | myex18.c | ✔              | ✔                                      | ✔          |                                             |
| `try catch`                        | myex19.c |                | ✔                                      | ✔          |                                             |
| `(**)`                             | myex20.c | ✔              | ✔                                      | ✔          |                                             |
| `Max Min`                          | myex21.c | ✔              | ✔                                      |            |                                             |
| `Abs`                              | myex21.c | ✔              | ✔                                      |            |                                             |
| `do until`                         | myex22.c | ✔              | ✔                                      | ✔          |                                             |
| return  静态作用域(本来有)         | myex23.c | ✔              | ✔ (本来有)                             | ✔ (本来有) |                                             |
|                                    |          |                |                                        |            |                                             |
|                                    |          |                |                                        |            |                                             |
|                                    |          |                |                                        |            |                                             |
|                                    |          |                |                                        |            |                                             |
|                                    |          |                |                                        |            |                                             |
|                                    |          |                |                                        |            |                                             |
|                                    |          |                |                                        |            |                                             |





##### 优化编译器查看中间过程

```shell
dotnet fsi
#r "nuget: FsLexYacc";;

#load "Absyn.fs"  "CPar.fs" "CLex.fs" "Debug.fs" "Parse.fs" "Machine.fs" "Backend.fs" "Contcomp.fs" "ParseAndContcomp.fs";;

open ParseAndContcomp;;

fromFile "example\myex1.c";;
contCompileToFile (fromFile "example\myex1.c") "myex1.out";;
```

