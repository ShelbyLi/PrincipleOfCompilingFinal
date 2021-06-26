(* File MicroC/Interp.c
   Interpreter for micro-C, a fraction of the C language
   sestoft@itu.dk * 2010-01-07, 2014-10-18

   A value is an integer; it may represent an integer or a pointer,
   where a pointer is just an address in the store (of a variable or
   pointer or the base address of an array).  The environment maps a
   variable to an address (location), and the store maps a location to
   an integer.  This freely permits pointer arithmetics, as in real C.
   Expressions can have side effects.  A function takes a list of
   typed arguments and may optionally return a result.

   For now, arrays can be one-dimensional only.  For simplicity, we
   represent an array as a variable which holds the address of the
   first array element.  This is consistent with the way array-type
   parameters are handled in C (and the way that array-type variables
   were handled in the B language), but not with the way array-type
   variables are handled in C.

   The store behaves as a stack, so all data are stack allocated:
   variables, function parameters and arrays.

   The return statement is not implemented (for simplicity), so all
   functions should have return type void.  But there is as yet no
   typecheck, so be careful.
 *)

module Interp

open Absyn
open Debug
open System

(* Simple environment operations *)
// 多态类型 env
// 环境 env 是 元组 ("name",data) 的列表 ，名称是字符串 string 值 'data 可以是任意类型
//  名称 ---> 数据 名称与数据绑定关系的 键-值 对  key-value pairs
// [("x",9);("y",8)]: int env

type 'data env = (string * 'data) list

//环境查找函数
//在环境 env上查找名称为 x 的值
let rec lookup env x =
    match env with
    | [] -> failwith (x + " not found")
    | (y, v) :: yr -> if x = y then v else lookup yr x


let rec structLookup env x index=
    match env with
    | []                            -> failwith(x + " not found")
    | (name, arglist, size)::rhs    -> if x = name then (index, arglist, size) else structLookup rhs x (index+1)


(* A local variable environment also knows the next unused store location *)

// ([("x",9);("y",8)],10)
// x 在位置9,y在位置8,10--->下一个空闲空间位置10
type locEnv = int env * int

(* A function environment maps a function name to parameter list and body *)
//函数参数例子:
//void func (int a , int *p)
// 参数声明列表为: [(TypI,"a");(TypP(TypI) ,"p")]
type paramdecs = (typ * string) list

(* 函数环境列表
  [("函数名", ([参数元组(类型,"名称")的列表],函数体AST)),....]

  //main (i){
  //  int r;
  //    fac (i, &r);
  //    print r;
  // }
  [ ("main",
   ([(TypI, "i")],
    Block
      [Dec (TypI,"r");
       Stmt (Expr (Call ("fac",[Access (AccVar "i"); Addr (AccVar "r")])));
       Stmt (Expr (Prim1 ("printi",Access (AccVar "r"))))]))]

函数环境 是 多态类型  'data env ---(string * 'data ) list 的一个 具体类型 ⭐⭐⭐
    类型变量 'data  具体化为  (paramdecs * stmt)
    (string * (paramdecs * stmt)) list
*)

type funEnv = (paramdecs * stmt) env


// type structEnv = (string *  typ * int ) list
type structEnv = (string *  paramdecs * int ) list

(* A global environment consists of a global variable environment
   and a global function environment
 *)

// 全局环境是 变量声明环境 和 函数声明环境的元组
// 两个列表的元组
// ([var declares...],[fun declares..])
// ( [ ("x" ,1); ("y",2) ], [("main",mainAST);("fac",facAST)] )
// mainAST,facAST 分别是main 与fac 的抽象语法树

type gloEnv = int env * funEnv

(* The store maps addresses (ints) to values (ints): *)

//地址是store上的的索引值
type address = int

// store 是一个 地址到值的映射，是对内存的抽象 ⭐⭐⭐
// store 是可更改的数据结构，特定位置的值可以修改，注意与环境的区别
// map{(0,3);(1,8) }
// 位置 0 保存了值 3
// 位置 1 保存了值 8

type store = Map<address, int>

//空存储
let emptyStore = Map.empty<address, int>

//保存value到存储store
let setSto (store: store) addr value = store.Add(addr, value)

//输入addr 返回存储的值value
let getSto (store: store) addr = store.Item addr

// store上从loc开始分配n个值的空间
// 用于数组分配
let rec initSto loc n store =
    if n = 0 then
        store
    else // 默认值 0
        initSto (loc + 1) (n - 1) (setSto store loc 0)

(* Combined environment and store operations *)

(* Extend local variable environment so it maps x to nextloc
   (the next store location) and set store[nextloc] = v.

locEnv结构是元组 : (绑定环境env,下一个空闲地址nextloc)
store结构是Map<string,int>

扩展环境 (x nextloc) :: env ====> 新环境 (env1,nextloc+1)
变更store (nextloc) = v
 *)

// 绑定一个值 x,v 到环境
// 环境是非更改数据结构，只添加新的绑定（变量名称，存储位置），注意与store 的区别⭐⭐⭐
// 返回新环境 locEnv,更新store,
// nextloc是store上下一个空闲位置
(*
    locEnv:
    ([(n, 5); (n, 4); (g, 0)], 6)

   store:
    (0, 0)  (1, 0)(2, 0)(3, 0)(4, 1)  (5, 8)
     ^^^^    ^^^^^^^^^^^^^^^^^^^^^^    ^^^^
       g               h                n

   变量 地址 值
   n--->5--->8
   h--->4--->1
   g--->0--->0

   下一个待分配位置是 6
*)

let bindVar x v (env, nextloc) store : locEnv * store =
    let env1 = (x, nextloc) :: env
    msg $"bindVar:\n%A{env1}\n"

    //返回新环境，新的待分配位置+1，设置当前存储位置为值 v
    ((env1, nextloc + 1), setSto store nextloc v)

//将多个值 xs vs绑定到环境
//遍历 xs vs 列表,然后调用 bindVar实现单个值的绑定
let store2str store =
    String.concat "" (List.map string (Map.toList store))

let rec bindVars xs vs locEnv store : locEnv * store =
    let res =
        match (xs, vs) with
        | ([], []) -> (locEnv, store)
        | (x1 :: xr, v1 :: vr) ->
            let (locEnv1, sto1) = bindVar x1 v1 locEnv store
            bindVars xr vr locEnv1 sto1
        | _ -> failwith "parameter/argument mismatch"

    msg "\nbindVars:\n"
    msg $"\nlocEnv:\n{locEnv}"
    msg $"\nStore:\n"
    store2str store |> msg
    res
(* Allocate variable (int or pointer or array): extend environment so
   that it maps variable to next available store location, and
   initialize store location(s).
 *)
//

let rec allocate (typ, x) (env0, nextloc) structEnv sto0 : locEnv * store =

    let (nextloc1, v, sto1) =
        match typ with
        //数组 调用 initSto 分配 i 个空间
        | TypA (t, Some i) -> (nextloc + i, nextloc, initSto nextloc i sto0)
        | TypS -> 
            (nextloc+128, nextloc, initSto nextloc 128 sto0)
            // allocate TypA (TypI Some 4) (env0, nextloc) sto0
            // (nextloc, 0, sto0)
        | TypStruct stru ->
            let (index, arg, size) = structLookup structEnv stru 0
            (nextloc+size, index, initSto nextloc size sto0)
        // 常规变量默认值是 0
        | _ -> (nextloc, 0, sto0)

    msg $"\nalloc:\n {((typ, x), (env0, nextloc), sto0)}"
    bindVar x v (env0, nextloc1) sto1

let typSize typ = 
    match typ with
    |  TypA (t, Some i) -> i
    |  TypS ->  128
    |  _ -> 1


(* Build global environment of variables and functions.  For global
   variables, store locations are reserved; for global functions, just
   add to global function environment.
*)

//初始化 解释器环境和store
// let initEnvAndStore (topdecs: topdec list) : locEnv * funEnv * store =

//     //包括全局函数和全局变量
//     msg $"\ntopdecs:\n{topdecs}\n"

//     let rec addv decs locEnv funEnv store =
//         match decs with
//         | [] -> (locEnv, funEnv, store)

//         // 全局变量声明  调用allocate 在store上给变量分配空间
//         | Vardec (typ, x) :: decr ->
//             let (locEnv1, sto1) = allocate (typ, x) locEnv store
//             addv decr locEnv1 funEnv sto1

//         | VardecAndAssign (typ, x, e) :: decr ->
//             let (locEnv1, sto1) = allocate (typ, x) locEnv store
//             let (v, sto2) = eval e locEnv gloEnv store
//             Assign (x, e) 
//             addv decr locEnv1 funEnv sto2
//         //全局函数 将声明(f,(xs,body))添加到全局函数环境 funEnv
//         | Fundec (_, f, xs, body) :: decr -> addv decr locEnv ((f, (xs, body)) :: funEnv) store

//     // ([], 0) []  默认全局环境
//     // locEnv ([],0) 变量环境 ，变量定义为空列表[],下一个空闲地址为0
//     // ([("n", 1); ("r", 0)], 2)  表示定义了 变量 n , r 下一个可以用的变量索引是 2
//     // funEnv []   函数环境，函数定义为空列表[]
//     addv topdecs ([], 0) [] emptyStore

(* ------------------------------------------------------------------- *)

(* Interpreting micro-C statements *)

let rec exec stmt (locEnv: locEnv) (gloEnv: gloEnv) (structEnv: structEnv) (store: store) : store =
    match stmt with
    | If (e, stmt1, stmt2) ->
        let (v, store1) = eval e locEnv gloEnv structEnv store

        if v <> 0 then
            exec stmt1 locEnv gloEnv structEnv store1 //True分支
        else
            exec stmt2 locEnv gloEnv structEnv store1 //False分支
    // | Switch (e, body) ->
    //     let (v, store1) = eval e locEnv gloEnv store
    //     // 定义辅助函数 pickCase
    //     let rec pickCase caseList =
    //         match caseList with
    //         | Case (e1, body1):: tail -> 
    //             let (caseV, caseStore) = eval e1 locEnv gloEnv store1
    //             if caseV <> v then
    //                 pickCase tail
    //             else  // 执行case的body
    //                 exec body1 locEnv gloEnv caseStore
    //         | [] -> store1
    //         | Default body1 :: tail->
    //             exec body1 locEnv gloEnv store1
    //         | _ -> failwith ("unknown grammar")
    //     pickCase body
    | Switch (e, body) ->
        let (v, store1) = eval e locEnv gloEnv structEnv store
        // 定义辅助函数 pickCase
        let rec pickCase caseList =
            match caseList with
            | Case (e1, body1):: tail -> 
                let (caseV, caseStore) = eval e1 locEnv gloEnv structEnv store1
                if caseV <> v then
                    pickCase tail
                else  // 执行case的body
                    exec body1 locEnv gloEnv structEnv caseStore
            | Default body1 :: tail->
                exec body1 locEnv gloEnv structEnv store1
            | [] -> store1
            // | _ -> failwith ("unknown grammar")
            
        pickCase body
    // | Case (e, body) -> exec body locEnv gloEnv store
    // | Default body -> exec body locEnv gloEnv store

    | While (e, body) ->

        //定义 While循环辅助函数 loop
        let rec loop store1 =
            //求值 循环条件,注意变更环境 store
            let (v, store2) = eval e locEnv gloEnv structEnv store1
            // match jumpOutStmt with
            // | Break -> store1
            // | Continue -> 
            // 继续循环
            if v <> 0 then
                loop (exec body locEnv gloEnv structEnv store2)
            else
                store2 //退出循环返回 环境store2

        loop store
    | For (e1, e2, e3, body) ->
        let (v1, store1) = eval e1 locEnv gloEnv structEnv store  // 计算e1
        let rec loop store1 =
            //求值 循环条件,注意变更环境 store
            let (v2, store2) = eval e2 locEnv gloEnv structEnv store1
            // 继续循环
            if v2 <> 0 then
                let store3 = exec body locEnv gloEnv structEnv store2
                let (v3, store4) = eval e3 locEnv gloEnv structEnv store3
                loop store4
            else
                store2 //退出循环返回 环境store2

        loop store1
    
    | DoWhile (body, e) ->
        let rec loop store1 =
            //求值 循环条件,注意变更环境 store
            let (v, store2) = eval e locEnv gloEnv structEnv store1
            // 继续循环
            if v <> 0 then
                loop (exec body locEnv gloEnv structEnv store2)
            else
                store2 //退出循环返回 环境store2

        loop (exec body locEnv gloEnv structEnv store)  // 先执行一遍body
    | DoUntil(body,e) -> 
        let rec loop store1 =
            let (v, store2) = eval e locEnv gloEnv structEnv  store1
            if v=0 then loop (exec body locEnv gloEnv structEnv  store2)
            else store2    
        loop (exec body locEnv gloEnv structEnv store)
    // | Break -> store
    // | Continue -> store

    | Expr e ->
        // _ 表示丢弃e的值,返回 变更后的环境store1
        let (_, store1) = eval e locEnv gloEnv structEnv store
        store1

    | Block stmts ->

        // 语句块 解释辅助函数 loop
        let rec loop ss (locEnv, store) =
            match ss with
            | [] -> store
            //语句块,解释 第1条语句s1
            // 调用loop 用变更后的环境 解释后面的语句 sr.
            | s1 :: sr -> loop sr (stmtordec s1 locEnv gloEnv structEnv store)

        loop stmts (locEnv, store)

    | Return e -> 
        // failwith "return not implemented" // 解释器没有实现 return
        match e with
        | Some e1 -> 
            let (res, store0) = eval e1 locEnv gloEnv structEnv store;
            // printfn "%s" (store0.ToString())
            let store1 = store0.Add(-1, res);
            store1
        | None -> store

and stmtordec stmtordec locEnv gloEnv structEnv store =
    match stmtordec with
    | Stmt stmt -> (locEnv, exec stmt locEnv gloEnv structEnv store)
    | Dec (typ, x) -> allocate (typ, x) locEnv structEnv store
    | DecAndAssign (typ, x, e) ->
        let (locEnv1, store1) = allocate (typ, x) locEnv structEnv store
        // let (res, store2) = eval e locEnv1 gloEnv store1
        let (res, store2) = eval (Assign(AccVar x, e)) locEnv1 gloEnv structEnv store1
        (locEnv1, store2)


(* Evaluating micro-C expressions *)

and eval e locEnv gloEnv structEnv store : int * store =
    match e with
    | ToInt e ->
        let (i, store1) = eval e locEnv gloEnv structEnv store
        if abs i > 100000000 then // float
            let bytes = System.BitConverter.GetBytes(int32(i))
            let v = System.BitConverter.ToSingle(bytes, 0)
            let res = int(round(v))
            // printf "welllll %d\n" res
            (res, store1)
        else
            (i, store1)
        // match e with
        // // | CstC c -> (int c - int '0', store)
        // | CstC c -> 
        //     printf "i am in\n"
        //     (int c, store)
        // | CstF f -> 
        //     let bytes = System.BitConverter.GetBytes(int32(f))
        //     let v = System.BitConverter.ToSingle(bytes, 0)
        //     printf "test %f\n" f
        //     (int v, store)
        // | _ -> 
        //     printf "not in\n"
        //     eval e locEnv gloEnv store
        // // | _ -> failwith "Could not change the type"


    | ToChar e ->
        let (i, store1) = eval e locEnv gloEnv structEnv store
        if abs i > 100000000 then // float
            let bytes = System.BitConverter.GetBytes(int32(i))
            let v = System.BitConverter.ToSingle(bytes, 0)
            let res = int(round(v))
            // printf "welllll %d\n" res
            (res, store1)
        else
            (i, store1)
        // match e with
        // // | CstC c -> (int c - int '0', store)
        // | CstC c -> (int c, store)
        // | CstF f -> 
        //     printf "test %d\n" (int f)
        //     (int f, store)
        // // | _ -> failwith "Could not change the type"
        // | _ -> eval e locEnv gloEnv store
    | PreInc acc -> 
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        let tmp = getSto store1 loc
        (tmp + 1, setSto store1 loc (tmp + 1)) 
    | PreDec acc -> 
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        let tmp = getSto store1 loc
        (tmp - 1, setSto store1 loc (tmp - 1)) 
    | NextInc acc ->
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        let tmp = getSto store1 loc
        (tmp, setSto store1 loc (tmp + 1))  // 先返回值 再加
    | NextDec acc -> 
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        let tmp = getSto store1 loc
        (tmp, setSto store1 loc (tmp - 1))
    | Access acc ->
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        (getSto store1 loc, store1)
    | Assign (acc, e) ->
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        // let (res, store2) = eval e locEnv gloEnv store1
        // printf "%d" loc

        let (res, store3) = 
            match e with
            | CstS s ->
                let mutable i = 0;
                let arrloc = getSto store1 loc  // 数组起始地址
                // printf "i am arrayloc %d\n" arrloc
                let mutable store2 = store1;
                while i < s.Length do
                    store2 <- setSto store2 (arrloc+i) (int (s.Chars(i)))
                    // printf "loc %d; " (arrloc+i)
                    // printf "assign %c\n"(s.Chars(i))
                    i <- i+1
                // printf "i am new arrayloc %d\n" (getSto store2 loc)
                // printf "i am new loc%d" loc
                (s.Length, store2)
            | _ ->  eval e locEnv gloEnv structEnv store1
        (loc, setSto store3 loc res) 
        // let (loc, store1) = access acc locEnv gloEnv store
        // let (res,store2)= 
        //   match e with
        //   | CstS s -> 
        //     let rec sign index stores=
        //         if index<s.Length then
        //           sign (index+1) ( setSto stores (loc-index-1) (int (s.Chars(index) ) ) )
        //         else stores  
        //     ( s.Length   ,sign 0 store1)
        //   | _ ->  eval e locEnv gloEnv store1
        // (res, setSto store2 loc res) 

    // | PlusAssign(acc, e) ->
    //     let (loc, store1) = access acc locEnv gloEnv store
    //     let tmp = getSto store1 loc
    //     let (res, store2) = eval e locEnv gloEnv store1
    //     (tmp + res, setSto store2 loc (tmp+res))
    // | MinusAssign(acc, e) ->
    //     let (loc, store1) = access acc locEnv gloEnv store
    //     let tmp = getSto store1 loc
    //     let (res, store2) = eval e locEnv gloEnv store1
    //     (tmp - res, setSto store2 loc (tmp-res))
    // | TimesAssign(acc, e) ->
    //     let (loc, store1) = access acc locEnv gloEnv store
    //     let tmp = getSto store1 loc
    //     let (res, store2) = eval e locEnv gloEnv store1
    //     (tmp * res, setSto store2 loc (tmp*res))
    // | DivAssign(acc, e) ->
    //     let (loc, store1) = access acc locEnv gloEnv store
    //     let tmp = getSto store1 loc
    //     let (res, store2) = eval e locEnv gloEnv store1
    //     (tmp / res, setSto store2 loc (tmp/res))
    // | ModAssign(acc, e) ->
    //     let (loc, store1) = access acc locEnv gloEnv store
    //     let tmp = getSto store1 loc
    //     let (res, store2) = eval e locEnv gloEnv store1
    //     (tmp % res, setSto store2 loc (tmp%res))
    | OpAssign (op, acc, e) ->
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        let tmp = getSto store1 loc
        let (res, store2) = eval e locEnv gloEnv structEnv store1
        let resValue =
            match op with
            | "+" -> tmp + res
            | "-" -> tmp - res
            | "*" -> tmp * res
            | "/" -> tmp / res
            | "%" -> tmp % res
            | _ -> failwith ("unknown primitive " + op)
        (resValue, setSto store2 loc resValue)
    | CstI i -> (i, store)
    | CstC c -> ((int c), store)
    | CstS s -> (s.Length, store)
        // let (loc, store1) = access (AccVar s) locEnv gloEnv store
        // (loc, store1)
        // (0, store)
    | CstF f -> 
        let bytes = System.BitConverter.GetBytes(float32(f))
        let v = System.BitConverter.ToInt32(bytes, 0)
        (v, store)
    // | CstNull -> (0 ,store)
    | Addr acc -> access acc locEnv gloEnv structEnv store
    | Prim1 (ope, e1) ->
        let (i1, store1) = eval e1 locEnv gloEnv structEnv store

        let res =
            match ope with
            | "!" -> if i1 = 0 then 1 else 0
            | "printi" ->
                (printf "%d " i1
                 i1)
            | "printc" ->
                (printf "%c" (char i1)
                 i1)
            | "~" -> ~~~i1
            | _ -> failwith ("unknown primitive " + ope)

        (res, store1)
    | Prim2 (ope, e1, e2) ->
        let (i1, store1) = eval e1 locEnv gloEnv structEnv store
        let (i2, store2) = eval e2 locEnv gloEnv structEnv store1

        let res =
            match ope with
            | "*" -> i1 * i2
            | "+" -> i1 + i2
            | "-" -> i1 - i2
            | "/" -> i1 / i2
            | "%" -> i1 % i2
            | "==" -> if i1 = i2 then 1 else 0
            | "!=" -> if i1 <> i2 then 1 else 0
            | "<" -> if i1 < i2 then 1 else 0
            | "<=" -> if i1 <= i2 then 1 else 0
            | ">=" -> if i1 >= i2 then 1 else 0
            | ">" -> if i1 > i2 then 1 else 0
            | "&" -> i1 &&& i2
            | "|" -> i1 ||| i2
            | "^" -> i1 ^^^ i2
            | "<<" -> i1 <<< i2
            | ">>" -> i1 >>> i2
            | _ -> failwith ("unknown primitive " + ope)

        (res, store2)
    | Prim3 (e1, e2, e3) ->
        let (v, store1) = eval e1 locEnv gloEnv structEnv store  // 求条件的值
        if v<>0 then eval e2 locEnv gloEnv structEnv store1  // true执行e2
                else eval e3 locEnv gloEnv structEnv store1  // false执行e3
    | Max (e1, e2) ->
        let (i1, store1) = eval e1 locEnv gloEnv structEnv store
        let (i2, store2) = eval e2 locEnv gloEnv structEnv store1
        let res = (if i1 > i2 then i1 else i2)
        (res, store2)
    | Min (e1, e2) ->
        let (i1, store1) = eval e1 locEnv gloEnv structEnv store
        let (i2, store2) = eval e2 locEnv gloEnv structEnv store1
        let res = (if i1 < i2 then i1 else i2)
        (res, store2)
    | Abs e ->
        let (i, store1) = eval e locEnv gloEnv structEnv store
        (abs(i), store1)
    | Printf (s, exprs) ->
        // let rec evalExprs exprs store1 =  // 循环计算printf后面所有表达式的值
        //     match exprs with
        //     | e :: tail ->  
        //         let (v, store2) = eval e locEnv gloEnv store1 
        //         let (vlist, store3) = evalExprs tail store2
        //         ([v] @ vlist, store3)
        //     | [] -> ([], store1)
        // let (evals, store1) = evalExprs exprs store
        let evalOneExpr exprs store1 =  // 返回计算得到的值, 剩下的exprs, 新的store
            match exprs with
            | e :: tail ->  
                let (v, store2) = eval e locEnv gloEnv structEnv store1 
                // let (vlist, store3) = evalExprs tail store2
                (v, tail, store2)
            | [] -> failwith "few expression"
        
        let getOneExpr exprs store1 =  // 返回计算得到的值, 剩下的exprs, 新的store
            match exprs with
            | e :: tail ->  
                // let (loc, store2) = access (Access e) locEnv gloEnv store1
                (e, tail, store1)
            | [] -> failwith "few expression"


        let mutable store1 = store
        let getPrintString =
            let slist = s.Split('%')
            let mutable resString = slist.[0]
            let mutable i = 1
            let mutable es = exprs
            // let mutable store1 = store
            while i < slist.Length do
                let printv =
                    match slist.[i].[0] with
                    | 'd' -> 
                        let (e, exprs2, store2) = evalOneExpr exprs store1
                        // let intv = 1
                        // if e.GetType().IsEquivalentTo((1).GetType()) then  // 检查类型是否是int..但是现在存的都是int ?
                            // evals.[i-1].ToString()
                        es <- exprs2
                        store1 <- store2
                        e.ToString()
                    | 'c' -> 
                        let (e, exprs2, store2) = evalOneExpr exprs store1
                        // char(evals.[i-1]).ToString()
                        es <- exprs2
                        store1 <- store2
                        char(e).ToString()
                    | 'f' -> 
                        let (e, exprs2, store2) = evalOneExpr exprs store1
                        es <- exprs2
                        store1 <- store2
                        let bytes = System.BitConverter.GetBytes(e)
                        let v = System.BitConverter.ToSingle(bytes, 0)
                        v.ToString()
                    | 's' ->
                        // let (slen, exprs2, store2) = oneExpr exprs store1
                        // printf "%d" slen
                        let (e, exprs2, store2) = getOneExpr exprs store1
                        let (loc, store3) = 
                            match e with
                            | Access acc -> access acc locEnv gloEnv structEnv store
                            | _ -> failwith "Don't support expression"
                        // printf "i m loc2 %d\n" loc
                        let arrloc = getSto store2 loc  // 数组起始地址
                        // printf "i am arrayloc2 %d\n" arrloc
                        // let (loc, store1) = access AccVar e locEnv gloEnv store
                        // let arrloc = getSto store1 loc
                        es <- exprs2
                        store1 <- store2
                        // let mutable store2 = store1;
                        let mutable i = 0;
                        let mutable s = ""
                        while i < arrloc do
                            // s <- s + char(getSto store2 (arrloc-i)).ToString()
                            s <- char(getSto store2 (arrloc-i-1)).ToString() + s
                            i <- i+1
                        // printf "i m s %s\n" s
                        s
                    | _ -> failwith "format mismatch"

                resString <- resString + printv + slist.[i].[1..]
                i <- i + 1
            printf "%s" resString
            1  // 返回1
        (getPrintString, store1)

    | Andalso (e1, e2) ->
        let (i1, store1) as res = eval e1 locEnv gloEnv structEnv store

        if i1 <> 0 then
            eval e2 locEnv gloEnv structEnv store1
        else
            res
    | Orelse (e1, e2) ->
        let (i1, store1) as res = eval e1 locEnv gloEnv structEnv store

        if i1 <> 0 then
            res
        else
            eval e2 locEnv gloEnv structEnv store1
    | Call (f, es) -> callfun f es locEnv gloEnv structEnv store

and access acc locEnv gloEnv structEnv store : int * store =
    match acc with
    | AccVar x -> (lookup (fst locEnv) x, store)
    | AccDeref e -> eval e locEnv gloEnv structEnv store
    | AccIndex (acc, idx) ->
        let (a, store1) = access acc locEnv gloEnv structEnv store
        let aval = getSto store1 a
        let (i, store2) = eval idx locEnv gloEnv structEnv store1
        (aval + i, store2)
    | AccStruct (acc, accMember) ->
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        let strct  = getSto store1 loc
        let memberlist = structEnv.[strct]

        let paramList =
            match memberlist with 
            | (string, paramdecs, int) -> paramdecs  // 得到该struct的parmdecs

        let rec getMemberIdx paramList index =  // 循环查找要找的变量名
            match paramList with
            | [] -> failwith("can not find ")
            | (typ , varName ) :: tail -> 
                match accMember with
                | AccVar x -> 
                    if x = varName then ( index + ( typSize typ ) )
                    else getMemberIdx tail ( index + ( typSize typ) )
                | AccIndex( accList, idx ) ->  
                    match accList with
                    | AccVar y ->  
                        if varName = y then 
                            let (i, store2) = eval idx locEnv gloEnv structEnv store1
                            (index + i)
                        else getMemberIdx tail (index + (typSize typ))
                    | _ -> failwith "fail"
                | _ -> failwith "fail"
        ((loc+(getMemberIdx paramList 0)), store1)


and evals es locEnv gloEnv structEnv store : int list * store =
    match es with
    | [] -> ([], store)
    | e1 :: er ->
        let (v1, store1) = eval e1 locEnv gloEnv structEnv store
        let (vr, storer) = evals er locEnv gloEnv structEnv store1
        (v1 :: vr, storer)

and callfun f es locEnv gloEnv structEnv store : int * store =

    msg
    <| sprintf "callfun: %A\n" (f, locEnv, gloEnv, store)

    let (_, nextloc) = locEnv
    let (varEnv, funEnv) = gloEnv
    let (paramdecs, fBody) = lookup funEnv f
    let (vs, store1) = evals es locEnv gloEnv structEnv store

    let (fBodyEnv, store2) =
        bindVars (List.map snd paramdecs) vs (varEnv, nextloc) store1

    let store3 = exec fBody fBodyEnv gloEnv structEnv store2
    // (-111, store3)
    let res = store3.TryFind(-1) 
    let restore = store3.Remove(-1)
    match res with
    | None -> (0,restore)
    | Some i -> (i,restore)

and initEnvAndStore (topdecs: topdec list) : locEnv * funEnv *  structEnv * store =

    //包括全局函数和全局变量
    msg $"\ntopdecs:\n{topdecs}\n"

    let rec addv decs locEnv funEnv structEnv store =
        match decs with
        | [] -> (locEnv, funEnv, structEnv, store)

        // 全局变量声明  调用allocate 在store上给变量分配空间
        | Vardec (typ, x) :: decr ->
            let (locEnv1, sto1) = allocate (typ, x) locEnv structEnv store
            addv decr locEnv1 funEnv structEnv sto1

        | VardecAndAssign (typ, x, e) :: decr ->
            let (locEnv1, sto1) = allocate (typ, x) locEnv structEnv store
            // let (v, sto2) = eval e locEnv ([], 0) store
            // let (v1, sto3) = Assign (x, v) 
            // addv decr locEnv1 funEnv sto3
            // let (res, store2) = eval (Assign(AccVar x, e)) locEnv1 gloEnv sto1
            addv decr locEnv1 funEnv structEnv sto1
        | Structdec (struName, memberlist) :: decr ->
            let rec getSize list strucSize = 
                match list with
                | [] -> strucSize
                | ( typ, memberName ):: tail -> getSize tail ((typSize typ) + strucSize)
            let size = getSize memberlist 0
            addv decr locEnv funEnv ((struName, memberlist, size) :: structEnv) store

        //全局函数 将声明(f,(xs,body))添加到全局函数环境 funEnv
        | Fundec (_, f, xs, body) :: decr -> addv decr locEnv ((f, (xs, body)) :: funEnv) structEnv store

    // ([], 0) []  默认全局环境
    // locEnv ([],0) 变量环境 ，变量定义为空列表[],下一个空闲地址为0
    // ([("n", 1); ("r", 0)], 2)  表示定义了 变量 n , r 下一个可以用的变量索引是 2
    // funEnv []   函数环境，函数定义为空列表[]
    addv topdecs ([], 0) [] [] emptyStore


(* Interpret a complete micro-C program by initializing the store
   and global environments, then invoking its `main' function.
 *)

// run 返回的结果是 代表内存更改的 store 类型
// vs 参数列表 [8,2,...]
// 可以为空 []
let run (Prog topdecs) vs =
    //
    let ((varEnv, nextloc), funEnv, structEnv, store0) = initEnvAndStore topdecs

    // mainParams 是 main 的参数列表
    //
    let (mainParams, mainBody) = lookup funEnv "main"

    let (mainBodyEnv, store1) =
        bindVars (List.map snd mainParams) vs (varEnv, nextloc) store0


    msg
    <|

    //以ex9.c为例子
    // main的 AST
    sprintf "\nmainBody:\n %A\n" mainBody
    +

    //局部环境
    // 如
    // i 存储在store位置0,store中下个空闲位置是1
    //([("i", 0)], 1)

    sprintf "\nmainBodyEnv:\n %A\n" mainBodyEnv
    +

    //全局环境 (变量,函数定义)
    // fac 的AST
    // main的 AST
    sprintf $"\n varEnv:\n {varEnv} \nfunEnv:\n{funEnv}\n"
    +

    //当前存储
    // store 中 0 号 位置存储值为8
    // map [(0, 8)]
    sprintf "\nstore1:\n %A\n" store1

    let endstore =
        exec mainBody mainBodyEnv (varEnv, funEnv) structEnv store1

    msg $"\nvarEnv:\n{varEnv}"
    msg $"\nStore:\n"
    msg <| store2str endstore

    endstore

(* Example programs are found in the files ex1.c, ex2.c, etc *)
