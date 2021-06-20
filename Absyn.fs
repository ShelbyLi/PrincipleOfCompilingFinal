(* File MicroC/Absyn.fs
   Abstract syntax of micro-C, an imperative language.
   sestoft@itu.dk 2009-09-25

   Must precede Interp.fs, Comp.fs and Contcomp.fs in Solution Explorer
 *)

module Absyn

// 基本类型
// 注意，数组、指针是递归类型
// 这里没有函数类型，注意与上次课的 MicroML 对比
type typ =
  | TypI                             (* Type int                    *)
  | TypC                             (* Type char                   *)
  | TypA of typ * int option         (* Array type                  *)
  | TypP of typ                      (* Pointer type                *)
                                                                   
and expr =                           // 表达式，右值                                                
  | Access of access                 (* x    or  *p    or  a[e]     *) //访问左值（右值）
  | Assign of access * expr          (* x=e  or  *p=e  or  a[e]=e   *)
  // | PlusAssign of access * expr      (* x+=e or  *p+=e or  a[e]+=e  *)
  // | MinusAssign of access * expr     (* x-=e or  *p-=e or  a[e]-=e  *)
  // | TimesAssign of access * expr     (* x*=e or  *p*=e or  a[e]*=e  *)
  // | DivAssign of access * expr       (* x/=e or  *p/=e or  a[e]/=e  *)
  // | ModAssign of access * expr       (* x%=e or  *p%=e or  a[e]%=e  *)
  | OpAssign of string * access * expr
  | Addr of access                   (* &x   or  &*p   or  &a[e]    *)
  | CstI of int                      (* Constant                    *)
  | CstC of char                     (* char类型  *)
  | Prim1 of string * expr           (* Unary primitive operator    *)
  | Prim2 of string * expr * expr    (* Binary primitive operator   *)
  | Prim3 of expr * expr * expr      (* 三目运算 e1 ? e2 : e3 *)
  | Printf of string * expr list     (* 格式化输出 *)
  | Andalso of expr * expr           (* Sequential and              *)
  | Orelse of expr * expr            (* Sequential or               *)
  | Call of string * expr list       (* Function call f(...)        *)
  | PreInc of access                 (* 自增 ++x or ++a[i]*)
  | PreDec of access                 (* 自减--x or --a[i]*)
  | NextInc of access                  (*x++ or a[i]--*)
  | NextDec of access                  (*x-- or a[i]--*)
                                                                   
and access =                         //左值，存储的位置                                            
  | AccVar of string                 (* Variable access        x    *) 
  | AccDeref of expr                 (* Pointer dereferencing  *p   *)
  | AccIndex of access * expr        (* Array indexing         a[e] *)
                                                                   
and stmt =                                                         
  | If of expr * stmt * stmt         (* Conditional                 *)
  | Switch of expr * caseStmt list       (* Switch case语句 case有多个 因此是个list*)
  // | Case of expr * stmt              (* Case  需要条件和要执行的语句*)
  // | Default of stmt                  (* Switch case 缺省语句 *)
  | While of expr * stmt             (* While loop                  *)
  | For of expr * expr * expr * stmt (* For循环 *)
  | DoWhile of stmt * expr           (* dowhile 循环*)
  | Expr of expr                     (* Expression statement   e;   *)
  | Return of expr option            (* Return from method          *)
  | Block of stmtordec list          (* Block: grouping and scope   *)
  // 语句块内部，可以是变量声明 或语句的列表                                                              

and caseStmt =  // switch case中用到的type
  | Case of expr * stmt
  | Default of stmt

and stmtordec =                                                    
  | Dec of typ * string              (* Local variable declaration  *)
  | Stmt of stmt                     (* A statement                 *)

// 顶级声明 可以是函数声明或变量声明
and topdec = 
  | Fundec of typ option * string * (typ * string) list * stmt
  | Vardec of typ * string

// 程序是顶级声明的列表
and program = 
  | Prog of topdec list
